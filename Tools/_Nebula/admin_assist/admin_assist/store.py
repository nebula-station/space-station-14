from __future__ import annotations

from collections.abc import Iterable, Sequence
from datetime import datetime
from uuid import UUID

import asyncpg

from .summary import PROMPT_VERSION, Summary
from .tracker import Ticket

GAME_VOTER_PREFIX = "ss14:"

_TICKET_COLUMNS = (
    "owner_user_id",
    "round_id",
    "server_id",
    "first_message_id",
    "last_message_id",
    "opened_at",
    "last_message_at",
    "opened_by_staff",
    "last_player_message_id",
    "last_player_message_at",
    "first_staff_reply_at",
    "handled_through_id",
    "summaries",
    "priority",
    "stuck_alert_at",
)


def _ticket_values(t: Ticket) -> tuple:
    return (
        t.owner, t.round_id, t.server_id, t.first_message_id, t.last_message_id, t.opened_at, t.last_message_at,
        t.opened_by_staff, t.last_player_message_id, t.last_player_message_at, t.first_staff_reply_at,
        t.handled_through_id, t.summaries, t.priority, t.stuck_alert_at,
    )  # fmt: skip


class Store:
    def __init__(self, pool: asyncpg.Pool) -> None:
        self._pool = pool


    async def cursor(self, name: str) -> tuple[int, int | None] | None:
        row = await self._pool.fetchrow("SELECT position, round_id FROM admin_assist.ingest_cursor WHERE name = $1", name)
        return (row["position"], row["round_id"]) if row else None

    async def set_cursor(self, name: str, position: int, round_id: int | None = None, *, conn=None) -> None:
        await (conn or self._pool).execute(
            """
            INSERT INTO admin_assist.ingest_cursor (name, position, round_id) VALUES ($1, $2, $3)
            ON CONFLICT (name) DO UPDATE SET position = $2, round_id = $3, updated_at = now()
            """,
            name,
            position,
            round_id,
        )


    async def save_tickets(self, tickets: Iterable[Ticket], *, cursor: tuple[str, int] | None = None) -> None:
        columns = ", ".join(_TICKET_COLUMNS)
        params = ", ".join(f"${n}" for n in range(1, len(_TICKET_COLUMNS) + 1))
        updates = ", ".join(f"{c} = EXCLUDED.{c}" for c in _TICKET_COLUMNS if c != "first_message_id")
        async with self._pool.acquire() as conn, conn.transaction():
            for ticket in tickets:
                ticket.ticket_id = await conn.fetchval(
                    f"""
                    INSERT INTO admin_assist.ahelp_ticket ({columns}) VALUES ({params})
                    ON CONFLICT (first_message_id) DO UPDATE SET {updates}
                    RETURNING ticket_id
                    """,
                    *_ticket_values(ticket),
                )
            if cursor is not None:
                await self.set_cursor(cursor[0], cursor[1], conn=conn)

    async def open_tickets(self, since: datetime, *, server_id: int | None = None) -> list[Ticket]:
        rows = await self._pool.fetch(
            f"SELECT ticket_id, {', '.join(_TICKET_COLUMNS)} FROM admin_assist.ahelp_ticket"
            " WHERE last_message_at >= $1 AND ($2::int IS NULL OR server_id = $2) ORDER BY first_message_id",
            since,
            server_id,
        )
        tickets = []
        for row in rows:
            data = dict(row)
            data["owner"] = data.pop("owner_user_id")
            tickets.append(Ticket(**data))
        return tickets


    async def add_summary(
        self,
        ticket: Ticket,
        summary: Summary,
        trigger: str,
        through_message_id: int,
        participants: Sequence[UUID] = (),
    ) -> int:
        async with self._pool.acquire() as conn, conn.transaction():
            await conn.execute(
                """
                UPDATE admin_assist.ahelp_ticket
                SET handled_through_id = $2, summaries = $3, priority = $4 WHERE ticket_id = $1
                """,
                ticket.ticket_id,
                ticket.handled_through_id,
                ticket.summaries,
                ticket.priority,
            )
            return await conn.fetchval(
                """
                INSERT INTO admin_assist.ahelp_summary
                    (ticket_id, through_message_id, trigger, model, prompt_version, text, priority, kind,
                     unreliable, problems, round_id, citations, attempts, cost_rub, participants)
                VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14, $15)
                RETURNING summary_id
                """,
                ticket.ticket_id,
                through_message_id,
                trigger,
                summary.model,
                PROMPT_VERSION,
                summary.text,
                summary.priority,
                summary.kind,
                summary.unreliable,
                list(summary.problems),
                ticket.round_id,
                {cite: list(ids) for cite, ids in summary.citations.items()},
                summary.attempts,
                summary.cost_rub,
                list(participants),
            )

    async def mark_briefing_started(self, ticket: Ticket, trigger: str) -> None:
        await self._pool.execute(
            "UPDATE admin_assist.ahelp_ticket SET briefing_started_at = now(), briefing_trigger = $2 WHERE ticket_id = $1",
            ticket.ticket_id,
            trigger,
        )

    async def mark_delivered(self, summary_id: int, ref: str) -> None:
        await self._pool.execute(
            "UPDATE admin_assist.ahelp_summary SET delivered_at = now(), delivery_ref = $2 WHERE summary_id = $1",
            summary_id,
            ref,
        )

    async def add_game_votes(self, votes: Iterable[asyncpg.Record]) -> int:
        rows = [(v["summary_id"], f"{GAME_VOTER_PREFIX}{v['admin_user_id']}", v["vote"], v["voted_at"]) for v in votes]
        if not rows:
            return 0
        result = await self._pool.fetchval(
            """
            WITH v AS (SELECT * FROM unnest($1::bigint[], $2::text[], $3::smallint[], $4::timestamptz[])
                       AS v(summary_id, voter, vote, at)),
            ins AS (
                INSERT INTO admin_assist.summary_feedback (summary_id, voter, vote, at)
                SELECT v.summary_id, v.voter, v.vote, v.at FROM v
                JOIN admin_assist.ahelp_summary s ON s.summary_id = v.summary_id
                WHERE v.vote IN (-1, 1)
                ON CONFLICT (summary_id, voter) DO NOTHING
                RETURNING 1
            )
            SELECT count(*) FROM ins
            """,
            *map(list, zip(*rows)),
        )
        return int(result)

