from __future__ import annotations

import httpx

from admin_assist.config import load_config
from admin_assist.modelwatch import ModelWatch, catalog_findings


def entry(model, **changes) -> dict:
    return {"id": model.id, "enabled": 1, "multiplier": model.multiplier, "demand_percent": 0, "demand_active": False, **changes}


def catalog(config, **per_tier) -> dict:
    models = []
    for tier, model in config.models.items():
        changes = per_tier.get(tier, {})
        if changes is not None:
            models.append(entry(model, **changes))
    return {"cache_rate": 0.5, "models": models + [{"id": "somebody-elses-model", "enabled": 0, "multiplier": 40}]}


def test_a_catalog_that_matches_the_config_has_nothing_to_report(config):
    assert catalog_findings(config, catalog(config)) == ({}, {})


def test_findings(config):
    main, fallback, small = (config.models[t] for t in ("main", "fallback", "small"))
    problems, surcharge = catalog_findings(
        config,
        catalog(
            config,
            main={"demand_percent": 25, "demand_active": True, "demand_until": "2026-11-11T20:00:00Z"},
            fallback={"multiplier": 3.0, "enabled": 0},
            small=None,
        ),
    )
    assert surcharge == {main.id: 1.25}
    assert set(problems) == {"main:demand:25", "fallback:disabled", "fallback:multiplier:3", "small:missing"}
    assert "+25%" in problems["main:demand:25"] and "2026-11-11T20:00:00Z" in problems["main:demand:25"]
    assert "admin_assist.model_fallback_multiplier" in problems["fallback:multiplier:3"]
    assert small.id in problems["small:missing"] and "admin_assist.model_small_id" in problems["small:missing"]
    assert fallback.id in problems["fallback:disabled"]


def test_surcharge_that_is_listed_but_not_active_does_not_count(config):
    assert catalog_findings(config, catalog(config, main={"demand_percent": 25, "demand_active": False})) == ({}, {})


def test_tiers_without_a_key_are_not_watched():
    config = load_config({"admin_assist.model_main_key": "key-main"})
    problems, _ = catalog_findings(config, {"models": []})
    assert set(problems) == {"main:missing"}


def test_odd_catalogs_do_not_raise(config):
    assert catalog_findings(config, {})[1] == {}
    assert catalog_findings(config, {"models": ["x", None, {"no": "id"}]})[1] == {}
    main = config.models["main"]
    odd = {"models": [{"id": main.id, "multiplier": "дорого", "demand_percent": "много", "demand_active": True}]}
    assert "main:missing" not in catalog_findings(config, odd)[0]


class Llm:
    def __init__(self) -> None:
        self.surcharge: dict[str, float] = {}

    def set_surcharge(self, model_id: str, factor: float) -> None:
        self.surcharge[model_id] = factor


class Harness:
    def __init__(self, config) -> None:
        self.config = config
        self.answer: object = catalog(config)
        self.alerts: list[str] = []
        self.llm = Llm()

        def handler(request: httpx.Request) -> httpx.Response:
            assert str(request.url) == config.kidmesh_catalog_url
            assert "authorization" not in request.headers
            if isinstance(self.answer, Exception):
                raise self.answer
            if isinstance(self.answer, httpx.Response):
                return self.answer
            return httpx.Response(200, json=self.answer)

        async def alert(text: str) -> None:
            self.alerts.append(text)

        self.watch = ModelWatch(config, httpx.AsyncClient(transport=httpx.MockTransport(handler)), self.llm, alert)


async def test_staff_hear_about_a_surcharge_once_and_again_when_it_ends(config):
    h = Harness(config)
    main = config.models["main"].id
    await h.watch.check()
    assert h.alerts == [] and h.llm.surcharge[main] == 1.0

    h.answer = catalog(config, main={"demand_percent": 40, "demand_active": True})
    await h.watch.check()
    await h.watch.check()
    assert len(h.alerts) == 1 and "+40%" in h.alerts[0]
    assert h.llm.surcharge[main] == 1.4

    h.answer = catalog(config, main={"demand_percent": 60, "demand_active": True})
    await h.watch.check()
    assert len(h.alerts) == 2 and "+60%" in h.alerts[1]

    h.answer = catalog(config)
    await h.watch.check()
    assert h.alerts[2:] == ["KIDMesh: наценка за спрос снята."]
    assert h.llm.surcharge[main] == 1.0


async def test_a_problem_that_stays_is_reported_once_and_again_if_it_comes_back(config):
    h = Harness(config)
    h.answer = catalog(config, fallback=None)
    await h.watch.check()
    await h.watch.check()
    assert len(h.alerts) == 1
    h.answer = catalog(config)
    await h.watch.check()
    h.answer = catalog(config, fallback=None)
    await h.watch.check()
    assert len(h.alerts) == 2


async def test_an_unreadable_catalog_changes_nothing(config):
    h = Harness(config)
    h.answer = catalog(config, main={"demand_percent": 40, "demand_active": True})
    await h.watch.check()
    main = config.models["main"].id
    for broken in (httpx.ConnectError("no route"), httpx.Response(503), httpx.Response(200, text="<html>"), httpx.Response(200, json=[1])):
        h.answer = broken
        await h.watch.check()
    assert len(h.alerts) == 1 and h.llm.surcharge[main] == 1.4


async def test_a_failing_alert_does_not_stop_the_watch(config):
    h = Harness(config)

    async def broken(text: str) -> None:
        raise RuntimeError("discord is down")

    h.watch._alert = broken
    h.answer = catalog(config, small=None)
    await h.watch.check()
