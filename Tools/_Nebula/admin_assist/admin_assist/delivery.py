from __future__ import annotations

import logging
from dataclasses import dataclass
from typing import Protocol

from .summary import UNRELIABLE, Summary
from .ticket import TicketMessage

log = logging.getLogger(__name__)


@dataclass(frozen=True)
class SummaryPost:
    owner_name: str
    round_number: int | None
    summary: Summary
    messages: tuple[TicketMessage, ...]
    number: int = 1


def format_summary(post: SummaryPost) -> str:
    head = f"Брифинг #{post.number} по тикету {post.owner_name}"
    if post.round_number is not None:
        head += f", раунд {post.round_number}"
    if post.summary.unreliable:
        head += f" ({UNRELIABLE}: {'; '.join(post.summary.problems)})"
    raw = "\n".join(f"[{m.time}] {m.sender}: {m.text}" for m in post.messages)
    return f"{head}\n{post.summary.text}\n--- тикет ---\n{raw}"


class Delivery(Protocol):
    async def summary(self, post: SummaryPost) -> str | None:
        ...

    async def alert(self, text: str, *, ping: bool = False) -> None:
        ...


class LogDelivery:
    async def summary(self, post: SummaryPost) -> str | None:
        log.info("SUMMARY\n%s", format_summary(post))
        return "log"

    async def alert(self, text: str, *, ping: bool = False) -> None:
        log.warning("ALERT: %s", text)
