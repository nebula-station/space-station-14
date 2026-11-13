from __future__ import annotations

from collections.abc import Iterable, Mapping
from dataclasses import dataclass
from datetime import datetime, timedelta
from uuid import UUID

from .config import Config

MAX_FAILURES = 2
RETRY_BACKOFF_S = 15.0
MAX_DETECTOR_CALLS = 4


@dataclass
class Ticket:
    owner: UUID
    round_id: int | None
    server_id: int | None
    first_message_id: int
    last_message_id: int
    opened_at: datetime
    last_message_at: datetime
    opened_by_staff: bool = False
    last_player_message_id: int | None = None
    last_player_message_at: datetime | None = None
    first_staff_reply_at: datetime | None = None
    handled_through_id: int | None = None
    summaries: int = 0
    priority: str | None = None
    stuck_alert_at: datetime | None = None
    ticket_id: int | None = None

    detector_verdict: tuple[int, bool] | None = None
    detector_calls: int = 0
    failures: int = 0
    retry_at: datetime | None = None
    busy: bool = False
    briefing: bool = False
    wanted: bool = False

    @property
    def pending(self) -> bool:
        last = self.last_player_message_id
        return last is not None and (self.handled_through_id is None or last > self.handled_through_id)


@dataclass(frozen=True)
class Action:
    kind: str
    ticket: Ticket
    trigger: str | None = None


class Tracker:
    def __init__(self, config: Config) -> None:
        self._gap = timedelta(seconds=config.ticket_gap_s)
        self._debounce = timedelta(seconds=config.detector_debounce_s)
        self._force = timedelta(seconds=config.detector_force_s)
        self._stale = timedelta(seconds=config.ticket_stale_s)
        self._max_summaries = config.ticket_max_summaries
        self._open: dict[tuple[UUID, int | None], Ticket] = {}

    @property
    def tickets(self) -> list[Ticket]:
        return list(self._open.values())

    def restore(self, tickets: Iterable[Ticket]) -> None:
        for ticket in tickets:
            self._open[(ticket.owner, ticket.round_id)] = ticket

    def get(self, owner: UUID, round_id: int | None) -> Ticket | None:
        return self._open.get((owner, round_id))

    def close(self, owner: UUID, round_id: int | None) -> Ticket | None:
        return self._open.pop((owner, round_id), None)

    def rebuild(self, owner: UUID, round_id: int | None, server_id: int | None, thread: Iterable[Mapping]) -> Ticket | None:
        self._open.pop((owner, round_id), None)
        ticket = None
        for row in thread:
            ticket = self.observe({**row, "ticket_owner_user_id": owner, "round_id": round_id, "server_id": server_id})
        return ticket

    def observe(self, message: Mapping) -> Ticket:
        key = (message["ticket_owner_user_id"], message["round_id"])
        msg_id, at, from_staff = message["ahelp_message_id"], message["sent_at"], message["sender_is_admin"]
        ticket = self._open.get(key)
        if ticket is None or at - ticket.last_message_at > self._gap:
            ticket = self._open[key] = Ticket(
                owner=key[0],
                round_id=key[1],
                server_id=message["server_id"],
                first_message_id=msg_id,
                last_message_id=msg_id,
                opened_at=at,
                last_message_at=at,
                opened_by_staff=from_staff,
            )
        ticket.last_message_id = max(ticket.last_message_id, msg_id)
        ticket.last_message_at = max(ticket.last_message_at, at)
        if from_staff:
            if ticket.first_staff_reply_at is None:
                ticket.first_staff_reply_at = at
        elif ticket.last_player_message_id is None or msg_id > ticket.last_player_message_id:
            ticket.last_player_message_id = msg_id
            ticket.last_player_message_at = at
        return ticket

    def _wants_summary(self, ticket: Ticket) -> bool:
        if ticket.opened_by_staff or not ticket.pending or ticket.summaries >= self._max_summaries:
            return False
        return ticket.summaries == 0 or ticket.first_staff_reply_at is None

    def due(self, now: datetime) -> list[Action]:
        actions = []
        for key, ticket in list(self._open.items()):
            if ticket.busy:
                continue
            if ticket.wanted:
                actions.append(Action("summarise", ticket, "manual"))
                continue
            if now - ticket.last_message_at > self._gap:
                del self._open[key]
                continue

            if self._wants_summary(ticket) and (ticket.retry_at is None or now >= ticket.retry_at):
                quiet = now - ticket.last_player_message_at
                last = ticket.last_player_message_id
                if quiet > self._stale:
                    actions.append(Action("give_up", ticket, "устарел"))
                elif ticket.failures > MAX_FAILURES:
                    actions.append(Action("give_up", ticket, "модель не отвечает"))
                elif ticket.detector_verdict == (last, True):
                    actions.append(Action("summarise", ticket, "detector"))
                elif quiet >= self._force:
                    actions.append(Action("summarise", ticket, "forced"))
                elif quiet >= self._debounce and ticket.detector_verdict != (last, False):
                    if ticket.detector_calls >= MAX_DETECTOR_CALLS:
                        actions.append(Action("summarise", ticket, "forced"))
                    else:
                        actions.append(Action("detect", ticket))
        return actions

    @staticmethod
    def failed(ticket: Ticket, now: datetime) -> None:
        ticket.failures += 1
        ticket.retry_at = now + timedelta(seconds=RETRY_BACKOFF_S * ticket.failures)
