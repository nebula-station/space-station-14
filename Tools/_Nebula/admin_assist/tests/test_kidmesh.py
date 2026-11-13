from __future__ import annotations

import json
from dataclasses import replace

import httpx
import pytest

from admin_assist.config import FeatureConfig
from admin_assist.kidmesh import BudgetExceeded, KidMesh, KidMeshError, fold_system
from admin_assist.ledger import LedgerEntry, MemoryLedger, Usage, estimate_cost_rub

MESSAGES = [{"role": "user", "content": "привет"}]


def ok(content="готово", *, prompt=1000, cached=0, completion=200, reasoning=None):
    message = {"role": "assistant", "content": content}
    if reasoning is not None:
        message["reasoning_content"] = reasoning
    return httpx.Response(
        200,
        json={
            "choices": [{"message": message}],
            "usage": {
                "prompt_tokens": prompt,
                "completion_tokens": completion,
                "prompt_tokens_details": {"cached_tokens": cached},
            },
        },
    )


class Harness:
    def __init__(self, config, responses, ledger=None, clock=None):
        self.responses = list(responses)
        self.requests: list[dict] = []
        self.keys: list[str] = []
        self.alerts: list[str] = []
        self.sleeps: list[float] = []
        self.ledger = ledger or MemoryLedger()

        def handler(request: httpx.Request) -> httpx.Response:
            self.requests.append(json.loads(request.content))
            self.keys.append(request.headers["authorization"])
            assert str(request.url) == "https://api.kidmesh.best/v1/chat/completions"
            item = self.responses.pop(0)
            if isinstance(item, Exception):
                raise item
            return item

        async def alert(message: str) -> None:
            self.alerts.append(message)

        async def sleep(seconds: float) -> None:
            self.sleeps.append(seconds)

        self.client = KidMesh(
            config,
            self.ledger,
            http=httpx.AsyncClient(transport=httpx.MockTransport(handler)),
            alert=alert,
            sleep=sleep,
            **({"clock": clock} if clock else {}),
        )

    @property
    def models(self):
        return [r["model"] for r in self.requests]


def test_cost_estimate():
    usage = Usage(prompt_tokens=1_000_000, cached_tokens=400_000, completion_tokens=100_000)
    assert estimate_cost_rub(usage, 1.0, 4.35, 0.5) == pytest.approx(0.9 * 4.35)
    assert estimate_cost_rub(usage, 2.5, 4.35, 0.5) == pytest.approx(0.9 * 4.35 * 2.5)


async def test_success_sends_effort_and_records_ledger(config):
    h = Harness(config, [ok(prompt=1000, cached=400, completion=200, reasoning="думаю")])
    result = await h.client.chat("ahelp_summary", MESSAGES, max_tokens=700)

    assert result.text == "готово"
    assert result.reasoning == "думаю"
    assert result.model == "deepseek-v4.1-flash-1017"
    assert not result.fell_back
    assert h.requests == [
        {"model": "deepseek-v4.1-flash-1017", "messages": MESSAGES, "reasoning_effort": "low", "max_tokens": 700}
    ]
    assert h.keys == ["Bearer key-main"]
    assert "tools" not in h.requests[0]

    [entry] = h.ledger.entries
    assert entry.feature == "ahelp_summary"
    assert entry.usage == Usage(1000, 400, 200)
    assert entry.cost_rub == pytest.approx((600 + 200 + 200) / 1e6 * 4.35)
    assert result.cost_rub == entry.cost_rub


async def test_small_model_gets_no_reasoning_effort(config):
    h = Harness(replace(config, features={**config.features, "done_typing": FeatureConfig("small", "none")}), [ok("DONE")])
    result = await h.client.chat("done_typing", MESSAGES)
    assert result.model == "gpt-6-luna"
    assert "reasoning_effort" not in h.requests[0]
    assert h.keys == ["Bearer key-small"]
    assert h.ledger.entries[0].cost_rub == pytest.approx(1200 / 1e6 * 4.35 * 0.9)


def test_fold_system():
    system = {"role": "system", "content": "правила"}
    user = {"role": "user", "content": "тикет"}
    assert fold_system([system, user]) == [{"role": "user", "content": "правила\n\nтикет"}]
    assert fold_system([user]) == [user]
    assert fold_system([system]) == [{"role": "user", "content": "правила"}]
    assert fold_system([system, {"role": "assistant", "content": "а"}, user]) == [
        {"role": "assistant", "content": "а"},
        {"role": "user", "content": "правила\n\nтикет"},
    ]
    assert user == {"role": "user", "content": "тикет"}


async def test_system_prompt_travels_inside_the_user_message(config):
    messages = [{"role": "system", "content": "инструкция"}, {"role": "user", "content": "данные"}]
    h = Harness(config, [ok()])
    await h.client.chat("ahelp_summary", messages)
    assert h.requests[0]["messages"] == [{"role": "user", "content": "инструкция\n\nданные"}]

    h = Harness(replace(config, kidmesh_fold_system=False), [ok()])
    await h.client.chat("ahelp_summary", messages)
    assert h.requests[0]["messages"] == messages


@pytest.mark.parametrize("status", [401, 402, 403])
async def test_rejected_key_or_empty_balance_pauses_the_tier_and_alerts_once(config, status):
    now = [1000.0]
    h = Harness(config, [httpx.Response(status, json={"error": {"message": "insufficient balance"}}), ok("запасная")], clock=lambda: now[0])
    result = await h.client.chat("ahelp_summary", MESSAGES)
    assert (result.model, result.fell_back) == ("glm-5.3", True)
    assert len(h.alerts) == 1 and f"HTTP {status}" in h.alerts[0] and "10 мин" in h.alerts[0]

    h.responses += [ok("запасная")]
    assert (await h.client.chat("ahelp_summary", MESSAGES)).model == "glm-5.3"
    assert h.models == ["deepseek-v4.1-flash-1017", "glm-5.3", "glm-5.3"]
    assert len(h.alerts) == 1

    now[0] += config.kidmesh_pause_s + 1
    h.responses += [ok("основная")]
    assert (await h.client.chat("ahelp_summary", MESSAGES)).model == "deepseek-v4.1-flash-1017"


async def test_demand_surcharge_raises_the_recorded_cost(config):
    h = Harness(config, [ok(prompt=1000, completion=0), ok(prompt=1000, completion=0)])
    h.client.set_surcharge("deepseek-v4.1-flash-1017", 1.5)
    assert (await h.client.chat("ahelp_summary", MESSAGES)).cost_rub == pytest.approx(1000 / 1e6 * 4.35 * 1.5)
    h.client.set_surcharge("deepseek-v4.1-flash-1017", 1.0)
    assert (await h.client.chat("ahelp_summary", MESSAGES)).cost_rub == pytest.approx(1000 / 1e6 * 4.35)


async def test_deepseek_style_cache_usage(config):
    response = httpx.Response(
        200,
        json={
            "choices": [{"message": {"content": "ок"}}],
            "usage": {"prompt_tokens": 100, "completion_tokens": 10, "prompt_cache_hit_tokens": 80},
        },
    )
    h = Harness(config, [response])
    result = await h.client.chat("ahelp_summary", MESSAGES)
    assert result.usage == Usage(100, 80, 10)


async def test_retries_transient_errors_then_succeeds(config):
    h = Harness(config, [httpx.Response(503, text="busy"), httpx.ConnectTimeout("slow"), ok()])
    result = await h.client.chat("ahelp_summary", MESSAGES)
    assert result.text == "готово"
    assert not result.fell_back
    assert h.models == ["deepseek-v4.1-flash-1017"] * 3
    assert h.sleeps == [0.5, 1.5]
    assert h.alerts == []


async def test_retry_after_is_honoured_but_capped(config):
    h = Harness(
        config,
        [httpx.Response(429, headers={"retry-after": "3"}), httpx.Response(429, headers={"retry-after": "600"}), ok()],
    )
    await h.client.chat("ahelp_summary", MESSAGES)
    assert h.sleeps == [3.0, 10.0]


@pytest.mark.parametrize(
    "response",
    [
        httpx.Response(404, json={"error": {"message": "The model `deepseek-v4.1-flash-1017` does not exist"}}),
        httpx.Response(400, json={"error": {"code": "model_not_found", "message": "nope"}}),
        httpx.Response(404, text="model not found"),
        httpx.Response(400, json={"error": {"message": "Модель недоступна или неверный запрос"}}),
    ],
)
async def test_pulled_model_falls_back_and_alerts(config, response):
    h = Harness(config, [response, ok("запасной")])
    result = await h.client.chat("ahelp_summary", MESSAGES)

    assert result.text == "запасной"
    assert result.fell_back
    assert result.model == "glm-5.3"
    assert h.models == ["deepseek-v4.1-flash-1017", "glm-5.3"]
    assert h.keys == ["Bearer key-main", "Bearer key-fallback"]
    assert h.requests[1]["reasoning_effort"] == "low"
    assert len(h.alerts) == 1 and "deepseek-v4.1-flash-1017" in h.alerts[0]
    [entry] = h.ledger.entries
    assert entry.fell_back and entry.model == "glm-5.3"
    assert entry.cost_rub == pytest.approx(1200 / 1e6 * 4.35 * 2.5)


async def test_exhausted_retries_fall_back(config):
    h = Harness(config, [httpx.Response(500)] * 3 + [ok("запасной")])
    result = await h.client.chat("ahelp_summary", MESSAGES)
    assert result.fell_back
    assert h.models == ["deepseek-v4.1-flash-1017"] * 3 + ["glm-5.3"]


async def test_auth_error_is_not_retried(config):
    h = Harness(config, [httpx.Response(401, json={"error": {"message": "bad key"}}), ok("запасной")])
    result = await h.client.chat("ahelp_summary", MESSAGES)
    assert result.fell_back
    assert h.sleeps == []


async def test_empty_answer_is_billed_then_falls_back(config):
    h = Harness(config, [ok(content=None), ok("запасной")])
    result = await h.client.chat("ahelp_summary", MESSAGES)
    assert result.text == "запасной"
    assert [e.model for e in h.ledger.entries] == ["deepseek-v4.1-flash-1017", "glm-5.3"]


async def test_all_tiers_failing_raises(config):
    h = Harness(config, [httpx.Response(401), httpx.Response(401)])
    with pytest.raises(KidMeshError) as e:
        await h.client.chat("ahelp_summary", MESSAGES)
    assert not isinstance(e.value, BudgetExceeded)
    assert h.ledger.entries == []


async def test_missing_key_skips_the_tier(config):
    from dataclasses import replace

    models = dict(config.models)
    models["main"] = replace(models["main"], api_key="")
    h = Harness(replace(config, models=models), [ok("запасной")])
    result = await h.client.chat("ahelp_summary", MESSAGES)
    assert result.fell_back and h.models == ["glm-5.3"]


async def test_budget_blocks_everything_but_exempt_features(config):
    ledger = MemoryLedger()
    await ledger.record(LedgerEntry("ahelp_summary", "m", Usage(), 500.0, False))
    h = Harness(config, [ok()], ledger=ledger)

    with pytest.raises(BudgetExceeded):
        await h.client.chat("done_typing", MESSAGES)
    assert h.requests == []

    assert (await h.client.chat("ahelp_summary", MESSAGES)).text == "готово"


async def test_budget_alerts_fire_once_per_threshold(config):
    ledger = MemoryLedger()
    await ledger.record(LedgerEntry("x", "m", Usage(), 399.999, False))
    h = Harness(config, [ok(), ok(), ok(), ok()], ledger=ledger)

    await h.client.chat("ahelp_summary", MESSAGES)
    await h.client.chat("ahelp_summary", MESSAGES)
    assert len(h.alerts) == 1 and "400" in h.alerts[0]

    await ledger.record(LedgerEntry("x", "m", Usage(), 100.0, False))
    await h.client.chat("ahelp_summary", MESSAGES)
    await h.client.chat("ahelp_summary", MESSAGES)
    assert len(h.alerts) == 2 and "исчерпан" in h.alerts[1]


class BrokenLedger(MemoryLedger):
    async def month_spend(self) -> float:
        raise ConnectionError("db down")

    async def record(self, entry) -> None:
        raise ConnectionError("db down")


async def test_ledger_outage_keeps_exempt_features_and_stops_the_rest(config):
    h = Harness(config, [ok()], ledger=BrokenLedger())
    assert (await h.client.chat("ahelp_summary", MESSAGES)).text == "готово"
    with pytest.raises(BudgetExceeded):
        await h.client.chat("done_typing", MESSAGES)
