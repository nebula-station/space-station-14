from __future__ import annotations

from collections.abc import Mapping, Sequence
from datetime import datetime, timedelta
from uuid import UUID

from .db import GameDb

MAX_NOTE_CHARS = 160
MAX_REASON_CHARS = 120


def plural(n: int, one: str, few: str, many: str) -> str:
    if n % 10 == 1 and n % 100 != 11:
        return one
    if n % 10 in (2, 3, 4) and n % 100 not in (12, 13, 14):
        return few
    return many


def duration_ru(span: timedelta | None) -> str:
    if span is None:
        return "навсегда"
    minutes = max(1, round(span.total_seconds() / 60))
    if minutes >= 24 * 60:
        days = round(minutes / (24 * 60))
        return f"{days} {plural(days, 'день', 'дня', 'дней')}"
    if minutes >= 60:
        hours = round(minutes / 60)
        return f"{hours} {plural(hours, 'час', 'часа', 'часов')}"
    return f"{minutes} {plural(minutes, 'минуту', 'минуты', 'минут')}"


def _cut(text: str, limit: int) -> str:
    text = " ".join(text.split())
    return text if len(text) <= limit else text[: limit - 1].rstrip() + "…"


def _day(at: datetime) -> str:
    return at.date().isoformat()


def ban_text(ban: Mapping) -> str:
    span = None if ban["expiration_time"] is None else ban["expiration_time"] - ban["ban_time"]
    if ban["type"] == 1:
        kind = "джоббан" + (f" ({', '.join(ban['roles'][:4])})" if ban["roles"] else "")
    else:
        kind = "бан"
    lifted = ", снят" if ban["unbanned"] else ""
    return f"{_day(ban['ban_time'])} {kind} {duration_ru(span)} за {_cut(ban['reason'], MAX_REASON_CHARS)}{lifted}"


def note_text(note: Mapping) -> str:
    kind = "вотчлист" if note["kind"] == "watchlist" else "заметка"
    return f'{_day(note["created_at"])} {kind} "{_cut(note["message"], MAX_NOTE_CHARS)}"'


def history_lines(
    order: Sequence[tuple[UUID, str]],
    *,
    notes: Sequence[Mapping],
    bans: Sequence[Mapping],
    alts: Sequence[Mapping],
    playtime_hours: Mapping[UUID, float],
    new_player_hours: float,
) -> list[str]:
    bans_by: dict[UUID, list[Mapping]] = {}
    for ban in bans:
        bans_by.setdefault(ban["user_id"], []).append(ban)
    notes_by: dict[UUID, list[Mapping]] = {}
    for note in notes:
        notes_by.setdefault(note["player_user_id"], []).append(note)

    lines = []
    for user_id, username in order:
        hours = playtime_hours.get(user_id, 0.0)
        if hours < new_player_hours:
            shown = f"{hours:.0f} ч" if hours >= 1 else "меньше часа"
            lines.append(f"{username}: новый игрок, наиграно {shown}")

        records = [(n["created_at"], note_text(n)) for n in notes_by.get(user_id, ())]
        records += [(b["ban_time"], ban_text(b)) for b in bans_by.get(user_id, ())]
        if records:
            records.sort(key=lambda r: r[0], reverse=True)
            lines.append(f"{username}: " + "; ".join(text for _, text in records))

        for alt in alts:
            if alt["user_id"] != user_id:
                continue
            other_bans = bans_by.get(alt["other_user_id"], [])
            if alt["kind"] == "ip" and not other_bans:
                continue
            what = "тот же HWID" if alt["kind"] == "hwid" else "тот же IP-адрес"
            detail = "; ".join(ban_text(b) for b in other_bans[:2]) or "банов нет"
            lines.append(f"{username}: {what}, что у {alt['other_user_name']} ({detail})")
    return lines


async def build_history(
    game: GameDb,
    order: Sequence[tuple[UUID, str]],
    *,
    scope_server_id: int | None,
    new_player_hours: float,
) -> list[str]:
    user_ids = [user_id for user_id, _ in order]
    if not user_ids:
        return []
    notes = await game.notes(user_ids, scope_server_id)
    alts = await game.alt_accounts(user_ids)
    with_alts = list({*user_ids, *(a["other_user_id"] for a in alts)})
    bans = await game.bans(with_alts, scope_server_id)
    playtime = await game.playtime_hours(user_ids)
    return history_lines(
        order, notes=notes, bans=bans, alts=alts, playtime_hours=playtime, new_player_hours=new_player_hours
    )
