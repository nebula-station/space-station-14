from __future__ import annotations

import logging
from collections.abc import Awaitable, Callable

import httpx

from .config import Config
from .kidmesh import KidMesh

log = logging.getLogger(__name__)

Alert = Callable[[str], Awaitable[None]]


def catalog_findings(config: Config, catalog: dict) -> tuple[dict[str, str], dict[str, float]]:
    by_id = {m.get("id"): m for m in catalog.get("models") or () if isinstance(m, dict)}
    problems: dict[str, str] = {}
    surcharge: dict[str, float] = {}
    for tier, model in config.models.items():
        if not model.api_key:
            continue
        entry = by_id.get(model.id)
        setting = f"admin_assist.model_{tier}"
        if entry is None:
            problems[f"{tier}:missing"] = f"KIDMesh: модели {model.id} ({tier}) больше нет в каталоге. Обновите {setting}_id."
            continue
        if not entry.get("enabled", 1):
            problems[f"{tier}:disabled"] = f"KIDMesh: модель {model.id} ({tier}) отключена в каталоге."
        try:
            multiplier = float(entry.get("multiplier"))
        except (TypeError, ValueError):
            multiplier = None
        if multiplier is not None and abs(multiplier - model.multiplier) > 1e-6:
            problems[f"{tier}:multiplier:{multiplier:g}"] = (
                f"KIDMesh: множитель модели {model.id} теперь ×{multiplier:g}, в настройках ×{model.multiplier:g}. "
                f"Обновите {setting}_multiplier, иначе учёт расходов врёт."
            )
        try:
            percent = float(entry.get("demand_percent") or 0)
        except (TypeError, ValueError):
            percent = 0.0
        if entry.get("demand_active") and percent > 0:
            surcharge[model.id] = 1 + percent / 100
            until = entry.get("demand_until")
            problems[f"{tier}:demand:{percent:g}"] = (
                f"KIDMesh: наценка за спрос +{percent:g}% на {model.id} ({tier})" + (f" до {until}." if until else ".")
            )
    return problems, surcharge


class ModelWatch:
    def __init__(self, config: Config, http: httpx.AsyncClient, llm: KidMesh, alert: Alert) -> None:
        self._config = config
        self._http = http
        self._llm = llm
        self._alert = alert
        self._known: dict[str, str] = {}

    async def check(self) -> None:
        url = self._config.kidmesh_catalog_url
        if not url:
            return
        try:
            response = await self._http.get(url, timeout=10)
            response.raise_for_status()
            catalog = response.json()
            if not isinstance(catalog, dict):
                raise ValueError("catalog is not an object")
        except (httpx.HTTPError, ValueError) as e:
            log.info("model catalog unavailable: %s: %s", type(e).__name__, e)
            return

        problems, surcharge = catalog_findings(self._config, catalog)
        for model in self._config.models.values():
            self._llm.set_surcharge(model.id, surcharge.get(model.id, 1.0))
        new = [text for key, text in problems.items() if key not in self._known]
        gone = [key for key in self._known if key not in problems]
        if any(":demand:" in key for key in gone) and not surcharge:
            new.append("KIDMesh: наценка за спрос снята.")
        self._known = problems
        for text in new:
            try:
                await self._alert(text)
            except Exception:
                log.exception("model watch: alert delivery failed: %s", text)
