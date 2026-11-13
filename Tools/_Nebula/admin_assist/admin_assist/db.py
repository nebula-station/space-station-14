from __future__ import annotations

import json
from collections.abc import Sequence
from datetime import datetime
from uuid import UUID

import asyncpg

from .logtypes import LogType

MAX_LOG_ROWS = 2000


async def _init_connection(conn: asyncpg.Connection) -> None:
    await conn.set_type_codec("jsonb", encoder=json.dumps, decoder=json.loads, schema="pg_catalog")


async def _pool(dsn: str, statement_timeout_ms: int, *, read_only: bool, max_size: int) -> asyncpg.Pool:
    settings = {"application_name": "admin_assist", "statement_timeout": str(statement_timeout_ms)}
    if read_only:
        settings["default_transaction_read_only"] = "on"
    return await asyncpg.create_pool(dsn, min_size=1, max_size=max_size, server_settings=settings, init=_init_connection)


async def own_pool(dsn: str, statement_timeout_ms: int = 5000) -> asyncpg.Pool:
    return await _pool(dsn, statement_timeout_ms, read_only=False, max_size=3)


class GameDb:
    def __init__(self, pool: asyncpg.Pool) -> None:
        self.pool = pool

    @classmethod
    async def connect(cls, dsn: str, statement_timeout_ms: int = 5000) -> GameDb:
        return cls(await _pool(dsn, statement_timeout_ms, read_only=True, max_size=3))

    async def close(self) -> None:
        await self.pool.close()

    async def round_logs(
        self,
        round_id: int,
        *,
        since: datetime | None = None,
        until: datetime | None = None,
        types: Sequence[int] | None = None,
        min_impact: int | None = None,
        player_ids: Sequence[UUID] | None = None,
        limit: int = 500,
        newest: bool = False,
    ) -> list[asyncpg.Record]:
        if not isinstance(round_id, int):
            raise TypeError("round_id is required: admin_log must always be filtered by round first")
        limit = max(1, min(limit, MAX_LOG_ROWS))
        order = "DESC" if newest else "ASC"
        rows = await self.pool.fetch(
            f"""
            SELECT l.admin_log_id, l.date, l.type, l.impact, l.message, l.json
            FROM admin_log l
            WHERE l.round_id = $1
              AND ($2::timestamptz IS NULL OR l.date >= $2)
              AND ($3::timestamptz IS NULL OR l.date <= $3)
              AND ($4::int[] IS NULL OR l.type = ANY($4))
              AND ($5::int IS NULL OR l.impact >= $5)
              AND ($6::uuid[] IS NULL OR EXISTS (
                    SELECT 1 FROM admin_log_player p
                    WHERE p.round_id = l.round_id AND p.log_id = l.admin_log_id
                      AND p.player_user_id = ANY($6)))
            ORDER BY l.admin_log_id {order}
            LIMIT $7
            """,
            round_id,
            since,
            until,
            list(types) if types is not None else None,
            min_impact,
            list(player_ids) if player_ids is not None else None,
            limit,
        )
        return rows[::-1] if newest else rows

    async def log_players(self, round_id: int, log_ids: Sequence[int]) -> dict[int, list[UUID]]:
        rows = await self.pool.fetch(
            "SELECT log_id, player_user_id FROM admin_log_player WHERE round_id = $1 AND log_id = ANY($2::int[])",
            round_id,
            list(log_ids)[:MAX_LOG_ROWS],
        )
        out: dict[int, list[UUID]] = {}
        for row in rows:
            out.setdefault(row["log_id"], []).append(row["player_user_id"])
        return out

    async def round(self, round_id: int) -> asyncpg.Record | None:
        return await self.pool.fetchrow(
            "SELECT round_id, number, server_id, start_date FROM round WHERE round_id = $1", round_id
        )

    async def round_started_at(self, round_id: int) -> datetime | None:
        return await self.pool.fetchval(
            "SELECT date FROM admin_log WHERE round_id = $1 AND type = $2 ORDER BY admin_log_id LIMIT 1",
            round_id,
            int(LogType.RoundStartJoin),
        )

    async def server_id(self, name: str) -> int | None:
        return await self.pool.fetchval("SELECT server_id FROM server WHERE name = $1", name)

    async def ahelp_messages(
        self,
        owner: UUID,
        round_id: int | None,
        *,
        since: datetime | None = None,
        until: datetime | None = None,
        limit: int = 200,
    ) -> list[asyncpg.Record]:
        rows = await self.pool.fetch(
            """
            SELECT ahelp_message_id, sender_user_id, sender_name, sender_is_admin, admin_only, from_discord,
                   text, sent_at
            FROM ahelp_message
            WHERE ticket_owner_user_id = $1
              AND round_id IS NOT DISTINCT FROM $2
              AND ($3::timestamptz IS NULL OR sent_at >= $3)
              AND ($4::timestamptz IS NULL OR sent_at <= $4)
            ORDER BY ahelp_message_id DESC
            LIMIT $5
            """,
            owner,
            round_id,
            since,
            until,
            limit,
        )
        return sorted(rows, key=lambda r: (r["sent_at"], r["ahelp_message_id"]))

    async def ahelp_max_id(self) -> int:
        return await self.pool.fetchval("SELECT coalesce(max(ahelp_message_id), 0) FROM ahelp_message")

    async def ahelp_messages_after(
        self, after_id: int, *, server_id: int | None = None, limit: int = 500
    ) -> list[asyncpg.Record]:
        return await self.pool.fetch(
            """
            SELECT ahelp_message_id, round_id, server_id, ticket_owner_user_id, sender_user_id, sender_name,
                   sender_is_admin, admin_only, from_discord, text, sent_at
            FROM ahelp_message
            WHERE ahelp_message_id > $1 AND ($2::int IS NULL OR server_id = $2)
            ORDER BY ahelp_message_id
            LIMIT $3
            """,
            after_id,
            server_id,
            limit,
        )

    async def briefing_votes_since(self, since: datetime) -> list[asyncpg.Record]:
        return await self.pool.fetch(
            "SELECT summary_id, admin_user_id, vote, voted_at FROM ahelp_briefing_vote WHERE voted_at >= $1"
            " ORDER BY voted_at LIMIT 5000",
            since,
        )

    async def ticket_requests_after(
        self, after_id: int, since: datetime, *, server_id: int | None = None, limit: int = 200
    ) -> list[asyncpg.Record]:
        return await self.pool.fetch(
            """
            SELECT ahelp_ticket_request_id, round_id, ticket_owner_user_id, admin_user_id, kind, requested_at
            FROM ahelp_ticket_request
            WHERE ahelp_ticket_request_id > $1 AND requested_at >= $2 AND ($3::int IS NULL OR server_id = $3)
            ORDER BY ahelp_ticket_request_id
            LIMIT $4
            """,
            after_id,
            since,
            server_id,
            limit,
        )

    async def players(self, user_ids: Sequence[UUID]) -> dict[UUID, asyncpg.Record]:
        rows = await self.pool.fetch(
            "SELECT user_id, last_seen_user_name, first_seen_time FROM player WHERE user_id = ANY($1::uuid[])",
            list(user_ids),
        )
        return {r["user_id"]: r for r in rows}

    async def playtime_hours(self, user_ids: Sequence[UUID]) -> dict[UUID, float]:
        rows = await self.pool.fetch(
            "SELECT player_id, extract(epoch FROM time_spent) / 3600.0 AS hours FROM play_time"
            " WHERE player_id = ANY($1::uuid[]) AND tracker = 'Overall'",
            list(user_ids),
        )
        return {r["player_id"]: float(r["hours"]) for r in rows}


    async def notes(self, user_ids: Sequence[UUID], scope_server_id: int | None, *, per_player: int = 4) -> list[asyncpg.Record]:
        return await self.pool.fetch(
            """
            SELECT player_user_id, kind, created_at, message FROM (
                SELECT *, row_number() OVER (PARTITION BY player_user_id ORDER BY created_at DESC) AS n FROM (
                    SELECT player_user_id, 'note' AS kind, created_at, message, server_id
                    FROM admin_notes WHERE player_user_id = ANY($1::uuid[]) AND NOT deleted
                    UNION ALL
                    SELECT player_user_id, 'watchlist', created_at, message, server_id
                    FROM admin_watchlists WHERE player_user_id = ANY($1::uuid[]) AND NOT deleted
                      AND (expiration_time IS NULL OR expiration_time > now())
                ) r
                WHERE $2::int IS NULL OR server_id IS NULL OR server_id = $2
            ) ranked
            WHERE n <= $3
            ORDER BY player_user_id, created_at DESC
            """,
            list(user_ids),
            scope_server_id,
            per_player,
        )

    async def bans(self, user_ids: Sequence[UUID], scope_server_id: int | None, *, per_player: int = 3) -> list[asyncpg.Record]:
        return await self.pool.fetch(
            """
            SELECT user_id, ban_id, type, ban_time, expiration_time, reason, unbanned, roles FROM (
                SELECT bp.user_id, b.ban_id, b.type, b.ban_time, b.expiration_time, b.reason,
                       EXISTS (SELECT 1 FROM unban u WHERE u.ban_id = b.ban_id) AS unbanned,
                       ARRAY(SELECT r.role_id FROM ban_role r WHERE r.ban_id = b.ban_id ORDER BY r.role_id) AS roles,
                       row_number() OVER (PARTITION BY bp.user_id ORDER BY b.ban_time DESC) AS n
                FROM ban_player bp JOIN ban b ON b.ban_id = bp.ban_id
                WHERE bp.user_id = ANY($1::uuid[])
                  AND ($2::int IS NULL OR b.server_id IS NULL OR b.server_id = $2)
            ) ranked
            WHERE n <= $3
            ORDER BY user_id, ban_time DESC
            """,
            list(user_ids),
            scope_server_id,
            per_player,
        )

    async def alt_accounts(self, user_ids: Sequence[UUID], *, per_player: int = 3) -> list[asyncpg.Record]:
        return await self.pool.fetch(
            """
            WITH me AS (
                SELECT user_id, last_seen_hwid AS hwid, COALESCE(last_seen_hwid_type, 0) AS hwid_type,
                       CASE WHEN last_seen_address << '127.0.0.0/8' OR last_seen_address = '::1'
                            THEN NULL ELSE last_seen_address END AS addr
                FROM player WHERE user_id = ANY($1::uuid[])
            ), found AS (
                SELECT me.user_id, o.user_id AS other_user_id, 0 AS weak
                FROM me JOIN player o ON o.last_seen_hwid = me.hwid AND COALESCE(o.last_seen_hwid_type, 0) = me.hwid_type
                UNION ALL
                SELECT me.user_id, bp.user_id, 0
                FROM me JOIN ban_hwid bh ON bh.hwid = me.hwid AND bh.hwid_type = me.hwid_type
                        JOIN ban_player bp ON bp.ban_id = bh.ban_id
                UNION ALL
                SELECT me.user_id, o.user_id, 1
                FROM me JOIN player o ON o.last_seen_address = me.addr
                UNION ALL
                SELECT me.user_id, bp.user_id, 1
                FROM me JOIN ban_address ba ON ba.address >>= me.addr
                        JOIN ban_player bp ON bp.ban_id = ba.ban_id
            ), best AS (
                SELECT user_id, other_user_id, min(weak) AS weak
                FROM found WHERE other_user_id <> user_id
                GROUP BY user_id, other_user_id
            ), ranked AS (
                SELECT best.*, p.last_seen_user_name AS other_user_name, p.last_seen_time,
                       row_number() OVER (PARTITION BY best.user_id ORDER BY best.weak, p.last_seen_time DESC) AS n
                FROM best JOIN player p ON p.user_id = best.other_user_id
            )
            SELECT user_id, other_user_id, other_user_name, CASE weak WHEN 0 THEN 'hwid' ELSE 'ip' END AS kind
            FROM ranked WHERE n <= $2
            ORDER BY user_id, weak, last_seen_time DESC
            """,
            list(user_ids),
            per_player,
        )
