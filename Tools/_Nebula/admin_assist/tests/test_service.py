from __future__ import annotations

from uuid import UUID

import httpx
import pytest

from admin_assist.kidmesh import KidMeshError
from admin_assist.logtypes import LogImpact, LogType
from admin_assist.service import REQUEST_BRIEFING, REQUEST_CLOSE, UNAVAILABLE_TEXT, Service

from .fakes import BRIEFING, FakeDelivery, FakeLLM
from .world import P, Q, ROUND, log, request, say, sec

RULES = "1.1 Не убивать без причины (тестовые правила)."
NO_CITES = BRIEFING.replace(" [L1]", "").replace(" [L2]", "")


class Clock:
    def __init__(self) -> None:
        self.at = sec(0)

    def __call__(self):
        return self.at


class Harness:
    def __init__(self, config, game, store, conn, **scripts) -> None:
        self.config, self.game, self.store, self.conn = config, game, store, conn
        self.llm = FakeLLM(**scripts)
        self.delivery = FakeDelivery()
        self.clock = Clock()
        self.service = self.new_service()

    def new_service(self) -> Service:
        return Service(self.config, self.game, self.store, self.llm, self.delivery, httpx.AsyncClient(), rules=RULES, now=self.clock)

    async def tick(self, at: float, *, service: Service | None = None) -> None:
        service = service or self.service
        self.clock.at = sec(at)
        await service.ahelp_tick()
        await service.drain()

    async def summaries(self) -> list[dict]:
        rows = await self.conn.fetch("SELECT * FROM admin_assist.ahelp_summary ORDER BY summary_id")
        return [dict(r) for r in rows]

    async def ticket(self) -> dict:
        return dict(await self.conn.fetchrow("SELECT * FROM admin_assist.ahelp_ticket ORDER BY ticket_id DESC LIMIT 1"))


@pytest.fixture
async def make(config, world, world_game, store):
    async def build(**scripts) -> Harness:
        harness = Harness(config, world_game, store, world, **scripts)
        await harness.service.start()
        return harness

    return build


pytestmark = pytest.mark.db


async def test_ticket_is_detected_summarised_stored_and_delivered(make, world):
    await log(world, 1, "Петя hit Вася using монтировка", -30, [Q, P], type_=LogType.Damaged, impact=LogImpact.Medium)
    await log(world, 2, "Петя caused Вася state to change from Alive to Critical", -20, [Q, P], type_=LogType.Damaged, impact=LogImpact.High)
    h = await make(done_typing="DONE", ahelp_summary=BRIEFING)

    first = await say(world, P, "меня убил petya", 0)
    last = await say(world, P, "вообще без причины", 1)
    await h.tick(2)
    assert h.llm.calls == []
    assert (await h.ticket())["last_player_message_id"] == last

    await h.tick(1 + h.config.detector_debounce_s)
    assert [f for f, _ in h.llm.calls] == ["done_typing"]
    await h.tick(2 + h.config.detector_debounce_s)
    assert [f for f, _ in h.llm.calls] == ["done_typing", "ahelp_summary"]
    prompt = h.llm.calls[1][1]
    assert RULES in prompt and "меня убил petya" in prompt and "[L2]" in prompt

    (post,) = h.delivery.summaries
    assert (post.owner_name, post.round_number, post.number) == ("vasya", 3, 1)
    assert [m.text for m in post.messages] == ["меня убил petya", "вообще без причины"]
    assert not post.summary.unreliable and post.summary.priority == "ОБЫЧНЫЙ"

    (row,) = await h.summaries()
    assert (row["trigger"], row["through_message_id"], row["unreliable"], row["delivery_ref"]) == ("detector", last, False, "msg-1")
    assert row["delivered_at"] is not None and row["round_id"] == ROUND
    ticket = await h.ticket()
    assert (ticket["first_message_id"], ticket["handled_through_id"], ticket["summaries"], ticket["priority"]) == (first, last, 1, "ОБЫЧНЫЙ")
    assert ticket["briefing_trigger"] == "detector" and ticket["briefing_started_at"] is not None

    for later in (30, 80, 300):
        await h.tick(later)
    assert len(h.llm.calls) == 2 and len(h.delivery.summaries) == 1 and h.delivery.alerts == []


async def test_wait_verdict_holds_the_summary_until_the_forced_timer(make, world):
    h = await make(done_typing="WAIT", ahelp_summary=NO_CITES)
    await say(world, P, "админ", 0)
    await h.tick(h.config.detector_debounce_s)
    await h.tick(30)
    await h.tick(h.config.detector_force_s - 1)
    assert [f for f, _ in h.llm.calls] == ["done_typing"]
    assert (await h.ticket())["briefing_started_at"] is None
    await h.tick(h.config.detector_force_s)
    assert h.llm.count("ahelp_summary") == 1
    assert (await h.summaries())[0]["trigger"] == "forced"
    assert (await h.ticket())["briefing_trigger"] == "forced"


async def test_detector_that_cannot_be_asked_costs_only_time(make, world):
    h = await make(done_typing=KidMeshError("down"), ahelp_summary=NO_CITES)
    await say(world, P, "админ", 0)
    await h.tick(h.config.detector_debounce_s)
    await h.tick(20)
    assert h.llm.count("done_typing") == 1 and h.delivery.alerts == []
    await h.tick(h.config.detector_force_s)
    assert len(h.delivery.summaries) == 1


async def test_briefing_that_cites_missing_lines_is_delivered_marked_unreliable(make, world):
    h = await make(done_typing="DONE", ahelp_summary=BRIEFING)
    await say(world, P, "админ", 0)
    await h.tick(10)
    await h.tick(11)
    assert h.llm.count("ahelp_summary") == 2
    (post,) = h.delivery.summaries
    assert post.summary.unreliable and post.summary.problems == ("ссылки на логи, которых нет: L1, L2",)
    (row,) = await h.summaries()
    assert row["unreliable"] and row["attempts"] == 2 and row["problems"] == list(post.summary.problems)


async def test_model_down_means_one_alert_a_few_tries_and_then_giving_up(make, world):
    down = KidMeshError("all models failed")
    h = await make(done_typing="DONE", ahelp_summary=[down, down, down, NO_CITES])
    first = await say(world, P, "админ", 0)
    await h.tick(10)
    await h.tick(11)
    assert h.delivery.alerts == [(UNAVAILABLE_TEXT, False)]
    await h.tick(12)
    assert h.llm.count("ahelp_summary") == 1
    await h.tick(11 + 15)
    await h.tick(26 + 30)
    assert h.llm.count("ahelp_summary") == 3
    await h.tick(56 + 45)
    assert h.llm.count("ahelp_summary") == 3 and h.delivery.summaries == []
    assert h.delivery.alerts == [(UNAVAILABLE_TEXT, False)]
    assert (await h.ticket())["handled_through_id"] == first and await h.summaries() == []

    second = await say(world, P, "ну хоть кто-нибудь", 150)
    await h.tick(160)
    await h.tick(161)
    assert len(h.delivery.summaries) == 1
    assert (await h.summaries())[0]["through_message_id"] == second


async def test_follow_up_briefings_stop_once_staff_are_in_the_thread(make, world):
    h = await make(done_typing="DONE", ahelp_summary=NO_CITES)
    await say(world, P, "админ", 0)
    await h.tick(10)
    await h.tick(11)
    await say(world, P, "ещё вот что", 20)
    await h.tick(30)
    await h.tick(31)
    assert [p.number for p in h.delivery.summaries] == [1, 2]
    assert [m.text for m in h.delivery.summaries[1].messages] == ["админ", "ещё вот что"]

    await say(world, P, "сейчас посмотрю", 40, admin=True)
    await say(world, P, "спасибо", 45)
    for at in (55, 56, 45 + h.config.detector_force_s):
        await h.tick(at)
    assert len(h.delivery.summaries) == 2 and h.llm.count("done_typing") == 2


async def test_thread_opened_by_staff_is_never_summarised(make, world):
    h = await make(done_typing="DONE", ahelp_summary=BRIEFING)
    await say(world, P, "зайди в дискорд", 0, admin=True)
    await say(world, P, "ок, а что случилось?", 5)
    for at in (15, 16, 5 + h.config.detector_force_s):
        await h.tick(at)
    assert h.llm.calls == [] and h.delivery.summaries == []
    assert (await h.ticket())["opened_by_staff"]


async def test_a_player_who_never_stops_writing_still_gets_a_briefing(make, world):
    h = await make(done_typing="WAIT", ahelp_summary=NO_CITES)
    step = h.config.detector_debounce_s + 1
    at = 0.0
    for n in range(12):
        at = n * step
        await say(world, P, f"и ещё {n}", at)
        await h.tick(at + h.config.detector_debounce_s)
        await h.tick(at + h.config.detector_debounce_s + 0.5)
    assert h.llm.count("ahelp_summary") >= 1
    assert h.llm.count("done_typing") <= 4 * h.config.ticket_max_summaries
    assert all(len(prompt) < 20_000 for _, prompt in h.llm.calls)


async def test_restart_neither_repeats_a_briefing_nor_drops_a_waiting_ticket(make, world):
    h = await make(done_typing="DONE", ahelp_summary=NO_CITES)
    await say(world, P, "админ", 0)
    await h.tick(10)
    await h.tick(11)
    await say(world, Q, "и мне помогите", 12)
    await h.tick(13)
    assert len(h.delivery.summaries) == 1

    again = h.new_service()
    await again.start()
    await h.tick(14, service=again)
    assert len(h.delivery.summaries) == 1
    await h.tick(12 + h.config.detector_debounce_s, service=again)
    await h.tick(13 + h.config.detector_debounce_s, service=again)
    assert [p.owner_name for p in h.delivery.summaries] == ["vasya", "petya"]
    assert len(await h.summaries()) == 2


async def test_summary_that_cannot_be_delivered_is_still_stored(make, world):
    h = await make(done_typing="DONE", ahelp_summary=NO_CITES)
    h.delivery.fail_summaries = True
    await say(world, P, "админ", 0)
    await h.tick(10)
    await h.tick(11)
    (row,) = await h.summaries()
    assert row["delivered_at"] is None and row["delivery_ref"] is None
    await h.tick(12)
    assert h.llm.count("ahelp_summary") == 1


async def test_in_game_votes_are_read_back_as_feedback(make, world):
    h = await make(done_typing="DONE", ahelp_summary=NO_CITES)
    await say(world, P, "админ", 0)
    await h.tick(10)
    await h.tick(11)
    summary_id = await world.fetchval("SELECT max(summary_id) FROM admin_assist.ahelp_summary")
    admin = UUID(int=77)

    async def feedback() -> dict:
        return {r["voter"]: r["vote"] for r in await world.fetch("SELECT voter, vote FROM admin_assist.summary_feedback")}

    from admin_assist.service import utcnow

    h.clock.at = utcnow()
    await world.execute(
        "INSERT INTO ahelp_briefing_vote (summary_id, admin_user_id, vote, voted_at) VALUES ($1, $2, -1, now()),"
        " ($3, $2, 1, now())",
        summary_id, admin, summary_id + 1000,
    )  # fmt: skip
    await h.service.feedback_tick()
    assert await feedback() == {f"ss14:{admin}": -1}
    await h.service.feedback_tick()
    assert await feedback() == {f"ss14:{admin}": -1}


async def test_only_the_configured_server_is_read(config, world, world_game, store):
    from dataclasses import replace

    h = Harness(replace(config, server_name="nebula"), world_game, store, world, done_typing="DONE", ahelp_summary=BRIEFING)
    await h.service.start()
    await say(world, Q, "я с другого сервера", 0, server_id=2, round_id=None)
    await h.tick(10)
    await h.tick(h.config.detector_force_s)
    assert h.llm.calls == []


async def test_closed_ticket_gets_a_new_briefing_when_the_player_writes_again(make, world):
    h = await make(done_typing="DONE", ahelp_summary=NO_CITES)
    await say(world, P, "меня убили", 0)
    await h.tick(10)
    await h.tick(11)
    await say(world, P, "разберусь", 20, admin=True)
    await request(world, P, REQUEST_CLOSE, 30)
    await h.tick(31)

    await say(world, P, "опять убили", 40)
    await h.tick(50)
    await h.tick(51)
    assert [p.number for p in h.delivery.summaries] == [1, 1]
    assert [m.text for m in h.delivery.summaries[1].messages] == ["опять убили"]


async def test_message_right_after_a_close_is_a_new_ticket_even_in_the_same_poll(make, world):
    h = await make(done_typing="DONE", ahelp_summary=NO_CITES)
    await say(world, P, "меня убили", 0)
    await h.tick(10)
    await h.tick(11)
    await say(world, P, "смотрю", 20, admin=True)
    await say(world, P, "опять убили", 32)
    await h.tick(33)
    await request(world, P, REQUEST_CLOSE, 30)
    await h.tick(43)
    await h.tick(44)
    assert [m.text for m in h.delivery.summaries[-1].messages] == ["опять убили"]
    assert len(h.delivery.summaries) == 2


async def test_briefing_being_made_when_the_ticket_is_closed_is_dropped(make, world):
    h = await make(done_typing="DONE", ahelp_summary=NO_CITES)
    await say(world, P, "меня убили", 0)
    await h.tick(10)
    ticket = h.service._tracker.get(P, ROUND)
    h.service._closed[(P, ROUND)] = sec(5)
    await h.service._summarise(ticket, "detector")
    assert h.delivery.summaries == [] and await h.summaries() == []


async def test_briefing_by_hand_of_an_already_briefed_ticket_does_nothing(make, world):
    h = await make(done_typing="DONE", ahelp_summary=NO_CITES)
    await say(world, P, "меня убили", 0)
    await h.tick(10)
    await h.tick(11)
    await say(world, P, "смотрю", 20, admin=True)
    await say(world, P, "вот ещё", 25)
    for at in (35, 36, 25 + h.config.detector_force_s):
        await h.tick(at)
    assert len(h.delivery.summaries) == 1

    await request(world, P, REQUEST_BRIEFING, 120)
    await h.tick(121)
    await h.tick(130)
    assert len(h.delivery.summaries) == 1


async def test_briefing_by_hand_of_a_ticket_that_went_quiet(make, world):
    h = await make(done_typing="DONE", ahelp_summary=NO_CITES)
    await say(world, P, "давно было", 0)
    later = 2 * h.config.ticket_gap_s
    await h.tick(later)
    assert h.service._tracker.get(P, ROUND) is None and h.delivery.summaries == []

    await request(world, P, REQUEST_BRIEFING, later + 1)
    await h.tick(later + 2)
    (post,) = h.delivery.summaries
    assert [m.text for m in post.messages] == ["давно было"]
    await h.tick(later + 3)
    assert h.service._tracker.get(P, ROUND) is None


async def test_briefing_by_hand_while_one_is_being_made_does_nothing(make, world):
    h = await make(done_typing="WAIT", ahelp_summary=NO_CITES)
    await say(world, P, "меня убили", 0)
    await h.tick(1)
    ticket = h.service._tracker.get(P, ROUND)
    ticket.briefing = True
    await request(world, P, REQUEST_BRIEFING, 2)
    await h.service.requests_tick()
    assert not ticket.wanted


async def test_old_or_foreign_requests_do_nothing(make, world):
    h = await make(done_typing="WAIT", ahelp_summary=NO_CITES)
    await say(world, P, "меня убили", 0)
    await say(world, Q, "и меня", 0, server_id=2)
    await request(world, P, REQUEST_BRIEFING, -600)
    await request(world, Q, REQUEST_BRIEFING, 0, server_id=2)
    h.service._server_id = 1
    await h.tick(1)
    assert h.llm.count("ahelp_summary") == 0
