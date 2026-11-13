from __future__ import annotations

import json
from uuid import uuid4

import pytest

from admin_assist.ingest import AHELP_CURSOR, OVERLAP, AhelpIngest
from admin_assist.summary import PROMPT_VERSION, Summary
from admin_assist.tracker import Ticket, Tracker

from .world import ADMIN, P, Q, ROUND, say, sec

pytestmark = pytest.mark.db


def ids(rows, key="ahelp_message_id") -> list[int]:
    return [r[key] for r in rows]


async def test_ahelp_queries(world, world_game):
    assert await world_game.ahelp_max_id() == 0
    a = await say(world, P, "раз", 0)
    b = await say(world, Q, "два", 1, server_id=2)
    c = await say(world, P, "три", 2, admin=True)
    assert await world_game.ahelp_max_id() == c
    assert ids(await world_game.ahelp_messages_after(0)) == [a, b, c]
    assert ids(await world_game.ahelp_messages_after(a)) == [b, c]
    assert ids(await world_game.ahelp_messages_after(0, server_id=1)) == [a, c]
    assert ids(await world_game.ahelp_messages_after(0, limit=2)) == [a, b]
    row = (await world_game.ahelp_messages_after(b))[0]
    assert (row["ticket_owner_user_id"], row["sender_user_id"], row["sender_is_admin"], row["round_id"], row["text"]) == (
        P, ADMIN, True, ROUND, "три",
    )  # fmt: skip


async def test_first_run_begins_at_the_present(world, world_game, store):
    await say(world, P, "старое", -500)
    old = await say(world, P, "тоже старое", -400)
    ingest = AhelpIngest(world_game, store)
    assert await ingest.poll() == []
    assert await store.cursor(AHELP_CURSOR) == (old, None)
    new = await say(world, P, "новое", 0)
    assert ids(await ingest.poll()) == [new]
    assert await ingest.poll() == [] and ingest.position == new


async def test_a_message_that_commits_late_is_still_handed_out(world, world_game, store):
    ingest = AhelpIngest(world_game, store)
    await ingest.start()
    await say(world, P, "второе, но видно первым", 1, msg_id=2)
    assert ids(await ingest.poll()) == [2]
    await say(world, P, "первое, закоммитилось позже", 0, msg_id=1)
    assert ids(await ingest.poll()) == [1]
    assert await ingest.poll() == [] and ingest.position == 2


async def test_restart_hands_out_nothing_twice_and_nothing_late_is_lost(world, world_game, store):
    first = AhelpIngest(world_game, store)
    await first.start()
    await say(world, P, "раз", 0, msg_id=1)
    await say(world, P, "три", 2, msg_id=3)
    assert ids(await first.poll()) == [1, 3]
    await store.set_cursor(AHELP_CURSOR, first.position)

    second = AhelpIngest(world_game, store)
    await second.start()
    assert await second.poll() == []
    await say(world, P, "два, поздно", 1, msg_id=2)
    await say(world, P, "четыре", 3, msg_id=4)
    assert ids(await second.poll()) == [2, 4]


async def test_messages_not_dealt_with_before_a_crash_come_again(world, world_game, store):
    first = AhelpIngest(world_game, store)
    await first.start()
    new = await say(world, P, "раз", 0)
    assert ids(await first.poll()) == [new]
    second = AhelpIngest(world_game, store)
    assert ids(await second.poll()) == [new]


async def test_other_servers_are_left_out(world, world_game, store):
    ingest = AhelpIngest(world_game, store, server_id=1)
    await ingest.start()
    mine = await say(world, P, "наш", 0)
    await say(world, Q, "чужой", 1, server_id=2)
    assert ids(await ingest.poll()) == [mine]


def chat_ids(found) -> list[tuple[int, int]]:
    return [(round_id, row["admin_log_id"]) for round_id, row in found]


def test_overlap_window_forgets_ids_it_can_no_longer_see():
    from admin_assist.ingest import _Window

    window = _Window(0)
    rows = [{"id": n} for n in range(1, OVERLAP * 3)]
    assert len(window.take(rows, "id")) == len(rows)
    assert window.position == OVERLAP * 3 - 1 and len(window._seen) == OVERLAP
    assert window.take(rows[-50:], "id") == []


def ticket(first: int = 1, at: float = 0, **changes) -> Ticket:
    base = dict(
        owner=P, round_id=ROUND, server_id=1, first_message_id=first, last_message_id=first, opened_at=sec(at),
        last_message_at=sec(at), last_player_message_id=first, last_player_message_at=sec(at),
    )  # fmt: skip
    return Ticket(**{**base, **changes})


SUMMARY = Summary("Приоритет: ВЫСОКИЙ\n…", "ВЫСОКИЙ", "жалоба на игрока", True, ("ссылки на логи, которых нет: L9",),
                  {"L1": (901,), "L2": (902, 903)}, 2, "fake-model", 0.0123)  # fmt: skip


async def test_cursors(store):
    assert await store.cursor("x") is None
    await store.set_cursor("x", 5)
    assert await store.cursor("x") == (5, None)
    await store.set_cursor("x", 9, 3)
    assert await store.cursor("x") == (9, 3)


async def test_tickets_are_saved_updated_and_restored(store):
    a, b = ticket(1, 0), ticket(5, 2000, owner=Q, round_id=None, opened_by_staff=True, last_player_message_id=None, last_player_message_at=None)
    await store.save_tickets([a, b], cursor=(AHELP_CURSOR, 5))
    assert a.ticket_id and b.ticket_id and a.ticket_id != b.ticket_id
    assert await store.cursor(AHELP_CURSOR) == (5, None)

    first_id = a.ticket_id
    a.last_message_id, a.last_message_at, a.first_staff_reply_at = 7, sec(30), sec(30)
    a.handled_through_id, a.summaries, a.priority, a.stuck_alert_at = 1, 1, "ВЫСОКИЙ", sec(150)
    a.busy, a.failures = True, 2
    await store.save_tickets([a])
    assert a.ticket_id == first_id
    assert await store.cursor(AHELP_CURSOR) == (5, None)

    restored = await store.open_tickets(sec(-10))
    assert [t.ticket_id for t in restored] == [a.ticket_id, b.ticket_id]
    a.busy, a.failures = False, 0
    assert restored == [a, b]
    assert await store.open_tickets(sec(31)) == [b]


async def test_ticket_and_cursor_move_together_or_not_at_all(store):
    await store.set_cursor(AHELP_CURSOR, 1)
    broken = ticket(2, 0, owner=None)
    with pytest.raises(Exception):
        await store.save_tickets([ticket(1, 0), broken], cursor=(AHELP_CURSOR, 2))
    assert await store.cursor(AHELP_CURSOR) == (1, None)
    assert await store.open_tickets(sec(-10)) == []


async def test_summary_is_stored_with_its_citations_and_the_ticket_state(admin_conn, store):
    t = ticket()
    await store.save_tickets([t])
    t.handled_through_id, t.summaries, t.priority = 4, 1, "ВЫСОКИЙ"
    owner, other = uuid4(), uuid4()
    summary_id = await store.add_summary(t, SUMMARY, "detector", 4, [owner, other])

    row = await admin_conn.fetchrow("SELECT * FROM admin_assist.ahelp_summary WHERE summary_id = $1", summary_id)
    assert (row["ticket_id"], row["through_message_id"], row["trigger"], row["model"], row["prompt_version"]) == (
        t.ticket_id, 4, "detector", "fake-model", PROMPT_VERSION,
    )  # fmt: skip
    assert (row["text"], row["priority"], row["kind"], row["unreliable"], row["attempts"], row["round_id"]) == (
        SUMMARY.text, "ВЫСОКИЙ", "жалоба на игрока", True, 2, ROUND,
    )  # fmt: skip
    assert row["problems"] == ["ссылки на логи, которых нет: L9"]
    assert json.loads(row["citations"]) == {"L1": [901], "L2": [902, 903]}
    assert float(row["cost_rub"]) == pytest.approx(0.0123)
    assert row["delivered_at"] is None and row["delivery_ref"] is None
    assert row["participants"] == [owner, other]

    (restored,) = await store.open_tickets(sec(-10))
    assert (restored.handled_through_id, restored.summaries, restored.priority) == (4, 1, "ВЫСОКИЙ")


async def test_the_store_role_cannot_read_the_game(store):
    import asyncpg

    with pytest.raises(asyncpg.InsufficientPrivilegeError):
        await store._pool.fetch("SELECT * FROM public.ahelp_message")


async def test_two_servers_on_one_database_keep_their_own_cursors_and_tickets(config, world, world_game, store):
    one, two = AhelpIngest(world_game, store, server_id=1), AhelpIngest(world_game, store, server_id=2)
    assert one.cursor != two.cursor != AHELP_CURSOR
    await one.start()
    await two.start()
    await say(world, P, "на первом", 0, server_id=1)
    await say(world, Q, "на втором", 0, server_id=2)
    tickets = [Tracker(config).observe(r) for r in await one.poll() + await two.poll()]
    await store.save_tickets(tickets)
    assert [t.owner for t in await store.open_tickets(sec(-1), server_id=2)] == [Q]
    assert len(await store.open_tickets(sec(-1))) == 2
