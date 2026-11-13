from __future__ import annotations

from datetime import datetime, timedelta, timezone
from uuid import UUID

import asyncpg
import pytest

from admin_assist import db as dbmod
from admin_assist.db import GameDb, own_pool
from admin_assist.ledger import LedgerEntry, PgLedger, Usage
from admin_assist.logtypes import LogImpact, LogType

from .conftest import RO_DSN, RW_DSN

pytestmark = pytest.mark.db

T0 = datetime(2026, 11, 1, 18, 0, tzinfo=timezone.utc)
ALICE = UUID("00000000-0000-0000-0000-00000000000a")
BOB = UUID("00000000-0000-0000-0000-00000000000b")
CAROL = UUID("00000000-0000-0000-0000-00000000000c")


@pytest.fixture
async def seeded(admin_conn):
    c = admin_conn
    await c.execute("INSERT INTO server (server_id, name) VALUES (1, 'nebula')")
    await c.execute("INSERT INTO round (round_id, number, server_id, start_date) VALUES (1, 1, 1, $1), (2, 2, 1, $1)", T0)
    for uid, name in ((ALICE, "alice"), (BOB, "bob"), (CAROL, "carol")):
        await c.execute(
            "INSERT INTO player (user_id, first_seen_time, last_seen_time, last_seen_user_name, last_seen_address)"
            " VALUES ($1, $2, $2, $3, '10.0.0.1')",
            uid, T0, name,
        )
    logs = [
        (1, LogType.Chat, LogImpact.Low, "Say from alice: привет", [ALICE]),
        (2, LogType.Damaged, LogImpact.Medium, "alice hit bob", [ALICE, BOB]),
        (3, LogType.Damaged, LogImpact.High, "bob caused alice state to change", [ALICE, BOB]),
        (4, LogType.Chat, LogImpact.Low, "OOC from carol: gg", [CAROL]),
        (5, LogType.Verb, LogImpact.Low, "carol used verb", [CAROL]),
        (6, LogType.Chat, LogImpact.Low, "Say from bob: ой", [BOB]),
    ]
    for log_id, type_, impact, message, players in logs:
        await c.execute(
            "INSERT INTO admin_log (round_id, admin_log_id, type, impact, date, message, json)"
            " VALUES (1, $1, $2, $3, $4, $5, $6)",
            log_id, int(type_), int(impact), T0 + timedelta(seconds=log_id), message, '{"k": 1}',
        )
        for uid in players:
            await c.execute("INSERT INTO admin_log_player (round_id, log_id, player_user_id) VALUES (1, $1, $2)", log_id, uid)
    await c.execute(
        "INSERT INTO admin_log (round_id, admin_log_id, type, impact, date, message, json)"
        " VALUES (2, 1, $1, -1, $2, 'other round', '{}')",
        int(LogType.Chat), T0,
    )


@pytest.fixture
async def game(seeded):
    g = await GameDb.connect(RO_DSN)
    yield g
    await g.close()


@pytest.fixture
async def own(admin_conn):
    pool = await own_pool(RW_DSN)
    yield pool
    await pool.close()


def ids(rows):
    return [r["admin_log_id"] for r in rows]


async def test_round_logs_are_scoped_to_the_round(game):
    rows = await game.round_logs(1)
    assert ids(rows) == [1, 2, 3, 4, 5, 6]
    assert rows[0]["message"] == "Say from alice: привет"
    assert rows[0]["json"] == {"k": 1}
    assert ids(await game.round_logs(2)) == [1]


async def test_round_id_is_mandatory(game):
    with pytest.raises(TypeError):
        await game.round_logs(None)


async def test_round_logs_filters(game):
    assert ids(await game.round_logs(1, types=[LogType.Chat])) == [1, 4, 6]
    assert ids(await game.round_logs(1, min_impact=LogImpact.Medium)) == [2, 3]
    assert ids(await game.round_logs(1, player_ids=[BOB])) == [2, 3, 6]
    assert ids(await game.round_logs(1, player_ids=[ALICE, CAROL], types=[LogType.Chat])) == [1, 4]
    assert ids(await game.round_logs(1, since=T0 + timedelta(seconds=3), until=T0 + timedelta(seconds=5))) == [3, 4, 5]
    assert ids(await game.round_logs(1, player_ids=[])) == []


async def test_round_logs_limit_keeps_oldest_or_newest(game, monkeypatch):
    assert ids(await game.round_logs(1, limit=2)) == [1, 2]
    assert ids(await game.round_logs(1, limit=2, newest=True)) == [5, 6]
    monkeypatch.setattr(dbmod, "MAX_LOG_ROWS", 3)
    assert ids(await game.round_logs(1, limit=10_000)) == [1, 2, 3]


async def test_log_players(game):
    players = await game.log_players(1, [2, 4])
    assert {k: sorted(v) for k, v in players.items()} == {2: [ALICE, BOB], 4: [CAROL]}


async def test_read_only_role_cannot_write_or_see_sidecar_schema(game):
    with pytest.raises(asyncpg.PostgresError):
        await game.pool.execute("DELETE FROM admin_log WHERE round_id = 1")
    async with game.pool.acquire() as conn:
        await conn.execute("SET default_transaction_read_only = off")
        try:
            with pytest.raises(asyncpg.InsufficientPrivilegeError):
                await conn.execute("UPDATE player SET server_currency = 999")
            with pytest.raises(asyncpg.InsufficientPrivilegeError):
                await conn.execute("CREATE TABLE public.sneaky (x int)")
            with pytest.raises(asyncpg.InsufficientPrivilegeError):
                await conn.fetch("SELECT * FROM admin_assist.llm_ledger")
        finally:
            await conn.execute("RESET default_transaction_read_only")


async def test_read_only_role_has_a_statement_timeout(seeded):
    conn = await asyncpg.connect(RO_DSN)
    try:
        assert await conn.fetchval("SHOW statement_timeout") == "5s"
        assert await conn.fetchval("SHOW default_transaction_read_only") == "on"
    finally:
        await conn.close()
    g = await GameDb.connect(RO_DSN, statement_timeout_ms=200)
    try:
        with pytest.raises(asyncpg.QueryCanceledError):
            await g.pool.execute("SELECT pg_sleep(2)")
    finally:
        await g.close()


async def test_owner_role_cannot_touch_game_tables(seeded, own):
    with pytest.raises(asyncpg.InsufficientPrivilegeError):
        await own.fetch("SELECT * FROM public.admin_log WHERE round_id = 1 LIMIT 1")
    with pytest.raises(asyncpg.InsufficientPrivilegeError):
        await own.fetch("SELECT last_seen_address FROM public.player LIMIT 1")
    with pytest.raises(asyncpg.InsufficientPrivilegeError):
        await own.execute("CREATE TABLE public.sneaky (x int)")


async def test_pg_ledger(admin_conn, own):
    ledger = PgLedger(own)
    assert await ledger.month_spend() == 0.0
    await ledger.record(LedgerEntry("ahelp_summary", "deepseek-v4.1-flash-1017", Usage(1000, 400, 200), 0.004350, False))
    await ledger.record(LedgerEntry("appeal", "glm-5.3", Usage(10, 0, 5), 1.25, True))
    assert await ledger.month_spend() == pytest.approx(1.25435)

    await admin_conn.execute(
        "INSERT INTO admin_assist.llm_ledger (at, feature, model, prompt_tokens, cached_tokens, completion_tokens, cost_rub)"
        " VALUES (date_trunc('month', now()) - interval '2 days', 'x', 'm', 0, 0, 0, 300)"
    )
    assert await ledger.month_spend() == pytest.approx(1.25435)

    assert await ledger.claim_alert(80) is True
    assert await ledger.claim_alert(80) is False
    assert await ledger.claim_alert(100) is True
