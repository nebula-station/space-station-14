from __future__ import annotations

import re
from collections.abc import Mapping, Sequence
from dataclasses import dataclass
from datetime import datetime, timedelta
from uuid import UUID

from .config import Config
from .db import MAX_LOG_ROWS, GameDb
from .history import build_history
from .logtypes import LogImpact, LogType
from .roster import RoundState
from .ticket import LogLine, Participant, RoundInfo, TicketContext, TicketMessage

EXCLUDED_TYPES = frozenset(
    {
        LogType.AdminFollow,
        LogType.AdminMessage,
        LogType.AdminCommands,
        LogType.GhostWarp,
        LogType.Connection,
        LogType.Vote,
        LogType.RateLimited,
        LogType.ChatRateLimited,
    }
)
EXCLUDED_CHAT_PREFIXES = (
    "Admin chat from",
    "Admin dead chat from",
    "Admin announcement",
    "Server announcement",
    "Server message to",
)
JOIN_TYPES = (LogType.RoundStartJoin, LogType.LateJoin)
PRIVATE_ERP_PREFIX = "Private ERP emote from"

_ENTITY = re.compile(r" \((\d+)/n\d+(?:, ([^,()]+))?(?:, ([^,()]+))?\)D?")
_STATE_CHANGE = re.compile(r"(state (?:to change|changed) from \w+ to \w+) at .*$", re.S)
_WORD = re.compile(r"[^\W\d_]{4,}")

_IMPACT_WEIGHT = {LogImpact.Low: 1, LogImpact.Medium: 2, LogImpact.High: 5, LogImpact.Extreme: 5}
MAX_MENTIONED = 3
MAX_SAME_JOB = 2
GENERIC_JOB_WORDS = frozenset({"глава", "главный", "старший", "младший", "службы", "станции", "отдела", "помощник"})
JOB_SLANG = {
    "кэп": "капитан",
    "кеп": "капитан",
    "гсб": "глава службы безопасности",
    "хос": "глава службы безопасности",
    "гп": "глава персонала",
    "хоп": "глава персонала",
    "смо": "главный врач",
    "главврач": "главный врач",
    "нр": "научный руководитель",
    "научрук": "научный руководитель",
    "си": "старший инженер",
    "км": "квартирмейстер",
}


def clean_log_message(message: str, json: Mapping | None, *, erp_text: bool = False) -> str:
    controlled = {}
    for value in (json or {}).values():
        if isinstance(value, dict) and isinstance(value.get("id"), int):
            controlled[value["id"]] = "player" in value

    def entity(match: re.Match) -> str:
        first, second = match.group(2), match.group(3)
        has_player = controlled.get(int(match.group(1)))
        if has_player is None:
            has_player = second is not None
        username = second or first
        return f" ({username})" if has_player and username else ""

    text = _ENTITY.sub(entity, message)
    text = _STATE_CHANGE.sub(r"\1", text)
    if not erp_text and text.startswith(PRIVATE_ERP_PREFIX):
        head, sep, _ = text.partition(": ")
        if sep:
            text = f"{head}: (текст скрыт)"
    return text


def is_excluded(row: Mapping) -> bool:
    if row["type"] in EXCLUDED_TYPES:
        return True
    return row["type"] == LogType.Chat and row["message"].startswith(EXCLUDED_CHAT_PREFIXES)


def rank_involved(author: UUID, rows: Sequence[Mapping], players_by_log: Mapping[int, Sequence[UUID]]) -> list[UUID]:
    score: dict[UUID, int] = {}
    last: dict[UUID, int] = {}
    for row in rows:
        if is_excluded(row):
            continue
        players = players_by_log.get(row["admin_log_id"], ())
        if author not in players:
            continue
        for other in players:
            if other != author:
                score[other] = score.get(other, 0) + _IMPACT_WEIGHT.get(row["impact"], 1)
                last[other] = row["admin_log_id"]
    return sorted(score, key=lambda u: (-score[u], -last[u]))


def _names_match(token: str, word: str) -> bool:
    need = max(4, len(token) - 2)
    return len(word) <= len(token) + 3 and len(word) >= need and word[:need] == token[:need]


def _job_words(job: str | None) -> set[str]:
    return set(_WORD.findall((job or "").lower())) - GENERIC_JOB_WORDS


MAX_MENTION_TEXT_CHARS = 2000
MAX_MENTION_TOTAL_CHARS = 8000


def mentioned_players(texts: Sequence[str], roster: RoundState, author: UUID) -> list[UUID]:
    joined = " ".join(t[:MAX_MENTION_TEXT_CHARS] for t in texts)[:MAX_MENTION_TOTAL_CHARS].lower()
    words = set(_WORD.findall(joined))
    others = [p for p in roster.players if p.user_id != author]

    by_name = []
    for p in others:
        hit = len(p.username) >= 3 and p.username.lower() in joined
        if not hit and p.character:
            tokens = _WORD.findall(p.character.lower())
            hit = any(_names_match(t, w) for t in tokens for w in words)
        if hit:
            by_name.append(p.user_id)

    said = {token for p in roster.players for token in _job_words(p.job) if any(_names_match(token, w) for w in words)}
    groups = [[p for p in roster.players if token in _job_words(p.job)] for token in sorted(said)]
    short_words = set(re.findall(r"[^\W\d_]{2,}", joined))
    groups += [
        [p for p in roster.players if job_part in (p.job or "").lower()]
        for alias, job_part in JOB_SLANG.items()
        if alias in short_words
    ]
    by_job = [p.user_id for holders in groups if len(holders) <= MAX_SAME_JOB for p in holders if p.user_id != author]

    return list(dict.fromkeys(by_name + by_job))[:MAX_MENTIONED]


@dataclass(frozen=True)
class _Group:
    at: datetime
    text: str
    ids: tuple[int, ...]
    tier: int


def _tier(row: Mapping, players: Sequence[UUID], participants: frozenset[UUID], author: UUID) -> int:
    involved = participants.intersection(players)
    if (author in involved and len(involved) >= 2) or _STATE_CHANGE.search(row["message"]):
        return 0
    if row["type"] in (LogType.Chat, LogType.Erp) or len(involved) >= 2:
        return 1
    if row["impact"] >= LogImpact.Medium:
        return 2
    return 3


def select_logs(
    rows: Sequence[Mapping],
    players_by_log: Mapping[int, Sequence[UUID]],
    participants: Sequence[UUID],
    author: UUID,
    *,
    cap: int,
    erp_text: bool = False,
) -> list[_Group]:
    wanted = frozenset(participants)
    groups: list[_Group] = []
    for row in rows:
        if is_excluded(row):
            continue
        text = clean_log_message(row["message"], row["json"], erp_text=erp_text)
        tier = _tier(row, players_by_log.get(row["admin_log_id"], ()), wanted, author)
        if groups and groups[-1].text == text:
            prev = groups[-1]
            groups[-1] = _Group(prev.at, text, (*prev.ids, row["admin_log_id"]), min(prev.tier, tier))
        else:
            groups.append(_Group(row["date"], text, (row["admin_log_id"],), tier))

    if len(groups) > cap:
        keep = sorted(reversed(groups), key=lambda g: g.tier)[:cap]
        groups = sorted(keep, key=lambda g: g.ids[0])
    return groups


def _clock(at: datetime, zero: datetime | None, *, seconds: bool = True) -> str:
    if zero is None:
        return at.strftime("%H:%M:%S" if seconds else "%H:%M")
    total = max(0, int((at - zero).total_seconds()))
    h, rest = divmod(total, 3600)
    m, s = divmod(rest, 60)
    return f"{h:02}:{m:02}:{s:02}" if seconds else f"{h:02}:{m:02}"


async def _joined_as(game: GameDb, round_id: int, user_ids: Sequence[UUID]) -> dict[UUID, tuple[str | None, str | None]]:
    if not user_ids:
        return {}
    rows = await game.round_logs(round_id, types=JOIN_TYPES, player_ids=user_ids, limit=4 * len(user_ids), newest=True)
    out: dict[UUID, tuple[str | None, str | None]] = {}
    for row in rows:
        data = row["json"] or {}
        entity = data.get("entity")
        try:
            user = UUID(entity["player"])
        except (TypeError, KeyError, ValueError):
            continue
        if isinstance(data.get("characterName"), str):
            out[user] = (data["characterName"], data.get("jobName") if isinstance(data.get("jobName"), str) else None)
    return out


async def build_context(
    game: GameDb,
    config: Config,
    *,
    owner: UUID,
    round_id: int | None,
    messages: Sequence[Mapping],
    roster: RoundState | None = None,
    scope_server_id: int | None = None,
) -> TicketContext:
    if not messages:
        raise ValueError("a ticket needs at least one message")
    if roster is not None and roster.round_id != round_id:
        roster = None
    on_server = roster.by_user() if roster else {}

    round_row = await game.round(round_id) if round_id is not None else None
    zero = await game.round_started_at(round_id) if round_row else None
    since = messages[0]["sent_at"] - timedelta(seconds=config.context_before_s)
    until = messages[-1]["sent_at"] + timedelta(seconds=config.context_after_s)

    involved: list[UUID] = [owner]
    if roster:
        player_texts = [m["text"] for m in messages if not m["sender_is_admin"]]
        involved += mentioned_players(player_texts, roster, owner)
    if round_row:
        own_rows = await game.round_logs(
            round_id, since=since, until=until, player_ids=[owner], limit=MAX_LOG_ROWS, newest=True
        )
        own_players = await game.log_players(round_id, [r["admin_log_id"] for r in own_rows])
        involved += [u for u in rank_involved(owner, own_rows, own_players) if u not in involved]
    involved = involved[: config.context_max_participants]

    groups: list[_Group] = []
    if round_row:
        rows = await game.round_logs(
            round_id, since=since, until=until, player_ids=involved, limit=MAX_LOG_ROWS, newest=True
        )
        players_by_log = await game.log_players(round_id, [r["admin_log_id"] for r in rows])
        groups = select_logs(
            rows, players_by_log, involved, owner, cap=config.context_max_log_lines, erp_text=config.erp_text_to_model
        )
    logs = tuple(
        LogLine(_clock(g.at, zero), g.text + (f" (×{len(g.ids)})" if len(g.ids) > 1 else ""), g.ids) for g in groups
    )

    known = await game.players(involved)
    missing = [u for u in involved if u not in on_server]
    joined = await _joined_as(game, round_id, missing) if round_row else {}
    ticket_names = {m["sender_user_id"]: m["sender_name"] for m in messages}
    participants = []
    for user in involved:
        live = on_server.get(user)
        if live:
            participants.append(
                Participant(live.username, user == owner, live.character, live.job, live.antag, live.state, user)
            )
            continue
        name = known[user]["last_seen_user_name"] if user in known else ticket_names.get(user, str(user))
        character, job = joined.get(user, (None, None))
        participants.append(Participant(name, user == owner, character, job, None, None, user))

    history = await build_history(
        game,
        [(p.user_id, p.username) for p in participants],
        scope_server_id=scope_server_id,
        new_player_hours=config.new_player_hours,
    )

    last = messages[-1]["sent_at"]
    info = RoundInfo(
        number=round_row["number"] if round_row else None,
        time=_clock(last, zero, seconds=False) if zero else None,
        gamemode=roster.gamemode if roster else None,
        player_count=roster.player_count if roster else None,
    )
    return TicketContext(
        round=info,
        messages=tuple(
            TicketMessage(
                _clock(m["sent_at"], zero), m["sender_name"], m["text"], m["sender_is_admin"], m["admin_only"]
            )
            for m in messages
        ),
        participants=tuple(participants),
        logs=logs,
        window=(_clock(since, zero), _clock(until, zero)) if round_row else None,
        history=tuple(history),
        round_id=round_id,
    )


async def record_scope(game: GameDb, config: Config) -> int | None:
    if not config.isolate_records:
        return None
    server_id = await game.server_id(config.server_name)
    if server_id is None:
        raise LookupError(f"nebula.isolate_records is on but no server is named {config.server_name!r} (adminlogs.server_name)")
    return server_id
