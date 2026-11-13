from __future__ import annotations

import json
from datetime import datetime, timedelta, timezone
from uuid import UUID

from admin_assist.logtypes import LogImpact, LogType

P = UUID("00000000-0000-0000-0000-0000000000a1")
Q = UUID("00000000-0000-0000-0000-0000000000b2")
ADMIN = UUID("00000000-0000-0000-0000-0000000000e5")
NAMES = {P: "vasya", Q: "petya", ADMIN: "BigBoss"}
T0 = datetime(2026, 11, 1, 18, 0, tzinfo=timezone.utc)
ROUND = 7


def sec(n: float) -> datetime:
    return T0 + timedelta(seconds=n)


async def create(c) -> None:
    await c.execute("INSERT INTO server (server_id, name) VALUES (1, 'nebula'), (2, 'other')")
    await c.execute("INSERT INTO round (round_id, number, server_id, start_date) VALUES ($1, 3, 1, $2)", ROUND, sec(-3600))
    for user, name in NAMES.items():
        await c.execute(
            "INSERT INTO player (user_id, first_seen_time, last_seen_time, last_seen_user_name, last_seen_address)"
            " VALUES ($1, $2, $2, $3, '10.0.0.1')",
            user, sec(-86400), name,
        )  # fmt: skip


async def say(c, owner: UUID, text: str, at: float, *, admin: bool = False, round_id: int | None = ROUND,
              server_id: int = 1, msg_id: int | None = None) -> int:  # fmt: skip
    sender = ADMIN if admin else owner
    columns = "round_id, server_id, ticket_owner_user_id, sender_user_id, sender_name, sender_is_admin, admin_only, from_discord, text, sent_at"
    values = "$1, $2, $3, $4, $5, $6, false, false, $7, $8"
    args = [round_id, server_id, owner, sender, NAMES[sender], admin, text, sec(at)]
    if msg_id is not None:
        columns, values = columns + ", ahelp_message_id", values + ", $9"
        args.append(msg_id)
    return await c.fetchval(f"INSERT INTO ahelp_message ({columns}) VALUES ({values}) RETURNING ahelp_message_id", *args)


async def request(c, owner: UUID, kind: int, at: float, *, round_id: int | None = ROUND, server_id: int = 1) -> None:
    await c.execute(
        "INSERT INTO ahelp_ticket_request (round_id, server_id, ticket_owner_user_id, admin_user_id, kind, requested_at)"
        " VALUES ($1, $2, $3, $4, $5, $6)",
        round_id, server_id, owner, ADMIN, kind, sec(at),
    )  # fmt: skip


async def log(c, log_id: int, message: str, at: float, players=(), *, type_: LogType = LogType.Chat,
              impact: LogImpact = LogImpact.Low, round_id: int = ROUND, data: dict | None = None) -> None:  # fmt: skip
    await c.execute(
        "INSERT INTO admin_log (round_id, admin_log_id, type, impact, date, message, json) VALUES ($1, $2, $3, $4, $5, $6, $7)",
        round_id, log_id, int(type_), int(impact), sec(at), message, json.dumps(data or {}),
    )  # fmt: skip
    for user in players:
        await c.execute("INSERT INTO admin_log_player (round_id, log_id, player_user_id) VALUES ($1, $2, $3)", round_id, log_id, user)
