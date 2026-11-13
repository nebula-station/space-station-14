from __future__ import annotations

import os

import asyncpg
import pytest

from admin_assist.config import Config, load_config
from admin_assist.db import GameDb, own_pool
from admin_assist.store import Store

from . import world as world_rows

ADMIN_DSN = os.environ.get("AA_TEST_ADMIN_DSN", "postgresql://ss14:local_game@127.0.0.1:55432/ss14")
RO_DSN = os.environ.get("AA_TEST_RO_DSN", "postgresql://admin_assist_ro:local_ro@127.0.0.1:55432/ss14")
RW_DSN = os.environ.get("AA_TEST_RW_DSN", "postgresql://admin_assist_rw:local_rw@127.0.0.1:55432/ss14")

TEST_CVARS = {
    "admin_assist.model_main_key": "key-main",
    "admin_assist.model_fallback_key": "key-fallback",
    "admin_assist.model_small_key": "key-small",
}


@pytest.fixture
def config() -> Config:
    return load_config(TEST_CVARS)


@pytest.fixture
async def admin_conn():
    try:
        conn = await asyncpg.connect(ADMIN_DSN, timeout=2)
    except (OSError, asyncpg.PostgresError, TimeoutError) as e:
        pytest.skip(f"local Postgres not reachable ({type(e).__name__}); run `docker compose up -d --wait`")
    await conn.execute(
        """
        TRUNCATE admin_log_player, admin_log, round, player, server, ban, play_time RESTART IDENTITY CASCADE;
        TRUNCATE ahelp_message, ahelp_briefing_vote, ahelp_ticket_request, connection_log, preference RESTART IDENTITY CASCADE;
        TRUNCATE admin_assist.llm_ledger, admin_assist.budget_alert, admin_assist.ingest_cursor,
                 admin_assist.ahelp_ticket, admin_assist.ahelp_summary, admin_assist.summary_feedback RESTART IDENTITY CASCADE;
        """
    )
    try:
        yield conn
    finally:
        await conn.close()


@pytest.fixture
async def world(admin_conn):
    await world_rows.create(admin_conn)
    return admin_conn


@pytest.fixture
async def world_game(world):
    game = await GameDb.connect(RO_DSN)
    yield game
    await game.close()


@pytest.fixture
async def store(admin_conn):
    pool = await own_pool(RW_DSN)
    yield Store(pool)
    await pool.close()
