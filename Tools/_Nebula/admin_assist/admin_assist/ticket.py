from __future__ import annotations

from dataclasses import dataclass, field
from uuid import UUID

STATES = ("lobby", "alive", "crit", "dead", "ghost")


@dataclass(frozen=True)
class RoundInfo:
    number: int | None
    time: str | None
    gamemode: str | None = None
    player_count: int | None = None


@dataclass(frozen=True)
class TicketMessage:
    time: str
    sender: str
    text: str
    is_admin: bool = False
    admin_only: bool = False


@dataclass(frozen=True)
class Participant:
    username: str
    is_author: bool = False
    character: str | None = None
    job: str | None = None
    antag: tuple[str, ...] | None = None
    state: str | None = None
    user_id: UUID | None = None


@dataclass(frozen=True)
class LogLine:
    time: str
    text: str
    log_ids: tuple[int, ...] = ()


@dataclass(frozen=True)
class TicketContext:
    round: RoundInfo
    messages: tuple[TicketMessage, ...]
    participants: tuple[Participant, ...]
    logs: tuple[LogLine, ...] = ()
    window: tuple[str, str] | None = None
    history: tuple[str, ...] = ()
    round_id: int | None = field(default=None, compare=False)

    def valid_citations(self) -> frozenset[str]:
        return frozenset(f"L{n}" for n in range(1, len(self.logs) + 1))
