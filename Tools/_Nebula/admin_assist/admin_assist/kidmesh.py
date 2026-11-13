from __future__ import annotations

import asyncio
import logging
import re
import time
from collections.abc import Awaitable, Callable
from dataclasses import dataclass

import httpx

from .config import Config, ModelConfig
from .ledger import Ledger, LedgerEntry, Usage, estimate_cost_rub

log = logging.getLogger(__name__)

RETRY_STATUSES = frozenset({408, 409, 425, 429, 500, 502, 503, 504, 520, 522, 524, 529})
MAX_RETRY_AFTER_S = 10.0
BLOCKED_STATUSES = {
    401: "ключ отклонён",
    402: "закончился баланс или лимит ключа",
    403: "доступ запрещён",
}
_MODEL_NOT_FOUND = re.compile(
    r"model.*(not found|does not exist|not exist|unknown|unavailable)|no such model"
    r"|модел\w*\s+(недоступн|не найден|отключен|не существует)",
    re.I | re.S,
)

Alert = Callable[[str], Awaitable[None]]


class KidMeshError(Exception):
    ...


class BudgetExceeded(KidMeshError):
    ...


class _TierFailed(Exception):
    def __init__(self, reason: str, model_not_found: bool = False, blocked: int | None = None) -> None:
        super().__init__(reason)
        self.model_not_found = model_not_found
        self.blocked = blocked


@dataclass(frozen=True)
class ChatResult:
    text: str
    reasoning: str | None
    model: str
    usage: Usage
    cost_rub: float
    fell_back: bool


async def _log_alert(message: str) -> None:
    log.warning("ALERT: %s", message)


def fold_system(messages: list[dict[str, str]]) -> list[dict[str, str]]:
    system = [m["content"] for m in messages if m["role"] == "system"]
    rest = [dict(m) for m in messages if m["role"] != "system"]
    if not system:
        return rest
    for m in rest:
        if m["role"] == "user":
            m["content"] = "\n\n".join([*system, m["content"]])
            return rest
    return [{"role": "user", "content": "\n\n".join(system)}, *rest]


def _parse_usage(raw: dict | None) -> Usage:
    raw = raw or {}
    details = raw.get("prompt_tokens_details") or {}
    cached = details.get("cached_tokens") or raw.get("prompt_cache_hit_tokens") or 0
    return Usage(
        prompt_tokens=int(raw.get("prompt_tokens") or 0),
        cached_tokens=int(cached),
        completion_tokens=int(raw.get("completion_tokens") or 0),
    )


def _is_model_not_found(status: int, body: dict | None, text: str) -> bool:
    error = (body or {}).get("error")
    if isinstance(error, dict):
        if error.get("code") == "model_not_found":
            return True
        message = str(error.get("message") or "")
    else:
        message = str(error or text)
    return status in (400, 404) and bool(_MODEL_NOT_FOUND.search(message))


class KidMesh:
    def __init__(
        self,
        config: Config,
        ledger: Ledger,
        *,
        http: httpx.AsyncClient | None = None,
        alert: Alert = _log_alert,
        sleep: Callable[[float], Awaitable[None]] = asyncio.sleep,
        clock: Callable[[], float] = time.monotonic,
    ) -> None:
        self._config = config
        self._ledger = ledger
        self._http = http or httpx.AsyncClient(timeout=config.kidmesh_timeout_s)
        self._alert = alert
        self._sleep = sleep
        self._clock = clock
        self._paused_until: dict[str, float] = {}
        self._surcharge: dict[str, float] = {}

    def set_surcharge(self, model_id: str, factor: float) -> None:
        if factor > 1:
            self._surcharge[model_id] = factor
        else:
            self._surcharge.pop(model_id, None)

    async def aclose(self) -> None:
        await self._http.aclose()

    async def chat(
        self,
        feature: str,
        messages: list[dict[str, str]],
        *,
        max_tokens: int | None = None,
        temperature: float | None = None,
    ) -> ChatResult:
        cfg = self._config
        fc = cfg.features[feature]
        await self._check_budget(feature)
        if cfg.kidmesh_fold_system:
            messages = fold_system(messages)

        tiers = [fc.tier] + (["fallback"] if fc.tier != "fallback" else [])
        errors: list[str] = []
        for index, tier in enumerate(tiers):
            model = cfg.models[tier]
            if not model.api_key:
                errors.append(f"{tier}: no API key configured")
                continue
            if self._clock() < self._paused_until.get(tier, 0.0):
                errors.append(f"{model.id}: paused after a rejected key or an empty balance")
                continue
            try:
                text, reasoning, usage = await self._call_tier(model, messages, fc.effort, max_tokens, temperature)
            except _TierFailed as e:
                errors.append(f"{model.id}: {e}")
                if e.model_not_found:
                    await self._safe_alert(f"KIDMesh: модель {model.id} не найдена (снята?), фича {feature}. Обновите admin_assist.model_{tier}_id.")
                elif e.blocked:
                    self._paused_until[tier] = self._clock() + cfg.kidmesh_pause_s
                    await self._safe_alert(
                        f"KIDMesh: {BLOCKED_STATUSES[e.blocked]} (HTTP {e.blocked}), модель {model.id}. "
                        f"Запросы к ней приостановлены на {cfg.kidmesh_pause_s / 60:.0f} мин. "
                        "Проверьте баланс и ключ в панели KIDMesh."
                    )
                else:
                    log.warning("KIDMesh %s failed for %s: %s", model.id, feature, e)
                continue

            multiplier = model.multiplier * self._surcharge.get(model.id, 1.0)
            cost = estimate_cost_rub(usage, multiplier, cfg.price_rub_per_mtok, cfg.cached_input_factor)
            fell_back = index > 0
            await self._record(LedgerEntry(feature, model.id, usage, cost, fell_back))
            if not text.strip():
                errors.append(f"{model.id}: empty response")
                continue
            return ChatResult(text, reasoning, model.id, usage, cost, fell_back)

        raise KidMeshError(f"{feature}: all model tiers failed: " + "; ".join(errors))

    async def _call_tier(
        self,
        model: ModelConfig,
        messages: list[dict[str, str]],
        effort: str,
        max_tokens: int | None,
        temperature: float | None,
    ) -> tuple[str, str | None, Usage]:
        payload: dict = {"model": model.id, "messages": messages}
        if effort != "none":
            payload["reasoning_effort"] = effort
        if max_tokens is not None:
            payload["max_tokens"] = max_tokens
        if temperature is not None:
            payload["temperature"] = temperature

        url = f"{self._config.kidmesh_base_url}/chat/completions"
        headers = {"Authorization": f"Bearer {model.api_key}"}
        attempts = self._config.kidmesh_max_retries + 1
        last = "no attempt made"
        for attempt in range(attempts):
            if attempt:
                await self._sleep(delay)
            delay = 0.5 * 3 ** attempt
            try:
                response = await self._http.post(url, json=payload, headers=headers)
            except httpx.TransportError as e:
                last = f"transport error: {type(e).__name__}"
                continue

            if response.status_code == 200:
                try:
                    body = response.json()
                    message = body["choices"][0]["message"]
                except (ValueError, LookupError, TypeError):
                    last = "malformed 200 response"
                    continue
                return message.get("content") or "", message.get("reasoning_content"), _parse_usage(body.get("usage"))

            try:
                body = response.json()
            except ValueError:
                body = None
            text = response.text[:200]
            if _is_model_not_found(response.status_code, body if isinstance(body, dict) else None, text):
                raise _TierFailed(f"model not found (HTTP {response.status_code})", model_not_found=True)
            last = f"HTTP {response.status_code}: {text}"
            if response.status_code in BLOCKED_STATUSES:
                raise _TierFailed(last, blocked=response.status_code)
            if response.status_code not in RETRY_STATUSES:
                raise _TierFailed(last)
            retry_after = response.headers.get("retry-after", "")
            if retry_after.replace(".", "", 1).isdigit():
                delay = min(float(retry_after), MAX_RETRY_AFTER_S)

        raise _TierFailed(f"gave up after {attempts} attempts; last: {last}")

    async def _check_budget(self, feature: str) -> None:
        cfg = self._config
        exempt = feature in cfg.budget_exempt
        try:
            spent = await self._ledger.month_spend()
        except Exception:
            log.exception("ledger unavailable while checking the budget")
            if exempt:
                return
            raise BudgetExceeded(f"{feature}: ledger unavailable, budget cannot be checked") from None
        if spent >= cfg.budget_rub and not exempt:
            raise BudgetExceeded(f"{feature}: monthly budget spent ({spent:.2f} of {cfg.budget_rub:.0f} ₽)")

    async def _record(self, entry: LedgerEntry) -> None:
        cfg = self._config
        try:
            await self._ledger.record(entry)
            spent = await self._ledger.month_spend()
            if spent >= cfg.budget_rub:
                if await self._ledger.claim_alert(100):
                    await self._safe_alert(
                        f"KIDMesh: месячный бюджет исчерпан ({spent:.0f} из {cfg.budget_rub:.0f} ₽). "
                        "Отключено всё, кроме сводок A-Help и проверки возраста."
                    )
            elif spent >= cfg.budget_rub * cfg.budget_alert_fraction:
                if await self._ledger.claim_alert(round(cfg.budget_alert_fraction * 100)):
                    await self._safe_alert(f"KIDMesh: потрачено {spent:.0f} из {cfg.budget_rub:.0f} ₽ месячного бюджета.")
        except Exception:
            log.exception("failed to record ledger entry %s", entry)

    async def _safe_alert(self, message: str) -> None:
        try:
            await self._alert(message)
        except Exception:
            log.exception("alert delivery failed: %s", message)
