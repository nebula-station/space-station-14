from __future__ import annotations

import re
from dataclasses import dataclass
from typing import Protocol

from .briefing import PRIORITIES, cited_ids, malformed_citations, parse_briefing, tidy, unknown_citations
from .kidmesh import ChatResult, KidMeshError
from .render import SUMMARY_PROMPT_VERSION, render_user_message
from .ticket import TicketContext

FEATURE = "ahelp_summary"
PROMPT_VERSION = SUMMARY_PROMPT_VERSION
UNRELIABLE = "ненадёжно"


class Chat(Protocol):
    async def chat(
        self, feature: str, messages: list[dict[str, str]], *, max_tokens: int | None = None, temperature: float | None = None
    ) -> ChatResult: ...


@dataclass(frozen=True)
class Summary:
    text: str
    priority: str | None
    kind: str | None
    unreliable: bool
    problems: tuple[str, ...]
    citations: dict[str, tuple[int, ...]]
    attempts: int
    model: str
    cost_rub: float
    uncited: bool = False


def problems_in(text: str, ctx: TicketContext) -> list[str]:
    return _check(text, ctx)[0]


def _check(text: str, ctx: TicketContext) -> tuple[list[str], int]:
    problems = []
    unknown = unknown_citations(text, ctx.valid_citations())
    if unknown:
        problems.append("ссылки на логи, которых нет: " + ", ".join(unknown))
    malformed = malformed_citations(text)
    if malformed:
        problems.append("ссылки на логи, которые не удалось прочитать: " + ", ".join(malformed))
    no_priority = parse_briefing(text).get("Приоритет") not in PRIORITIES
    if no_priority:
        problems.append("нет строки приоритета")
    return problems, len(unknown) + len(malformed) + no_priority


def _uncited(text: str, ctx: TicketContext) -> bool:
    claims = parse_briefing(text).get("Логи", "")
    if not ctx.logs or not claims or re.search(r"\bнет\b", claims.lower()):
        return False
    return not set(cited_ids(claims)) & ctx.valid_citations()


async def summarise(llm: Chat, system_prompt: str, ctx: TicketContext, *, max_tokens: int | None = None) -> Summary:
    best: tuple[int, list[str], str, ChatResult] | None = None
    cost = 0.0
    attempts = 0
    for attempts in (1, 2):
        messages = [
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": render_user_message(ctx)},
        ]
        try:
            result = await llm.chat(FEATURE, messages, max_tokens=max_tokens)
        except KidMeshError:
            if best is None:
                raise
            attempts -= 1
            break
        cost += result.cost_rub
        text = tidy(result.text)
        problems, badness = _check(text, ctx)
        if best is None or badness < best[0]:
            best = (badness, problems, text, result)
        if not problems:
            break

    _, problems, text, result = best
    fields = parse_briefing(text)
    priority = fields.get("Приоритет")
    valid = ctx.valid_citations()
    return Summary(
        text=text,
        priority=priority if priority in PRIORITIES else None,
        kind=fields.get("Тип", "").lower() or None,
        unreliable=bool(problems),
        problems=tuple(problems),
        citations={c: ctx.logs[int(c[1:]) - 1].log_ids for c in cited_ids(text) if c in valid},
        attempts=attempts,
        model=result.model,
        cost_rub=cost,
        uncited=_uncited(text, ctx),
    )
