from __future__ import annotations

import logging
import re
from collections.abc import Sequence

from .kidmesh import KidMeshError
from .render import detector_system_prompt, render_detector_message
from .summary import Chat
from .ticket import TicketMessage

log = logging.getLogger(__name__)

FEATURE = "done_typing"
MAX_TOKENS = 300

_VERDICT = re.compile(r"\b(DONE|WAIT)\b")


def parse_verdict(text: str) -> bool:
    match = _VERDICT.search(text.upper())
    return bool(match) and match.group(1) == "DONE"


async def finished_typing(llm: Chat, messages: Sequence[TicketMessage], seconds_since_last: float) -> bool:
    prompt = [
        {"role": "system", "content": detector_system_prompt()},
        {"role": "user", "content": render_detector_message(list(messages), seconds_since_last)},
    ]
    try:
        result = await llm.chat(FEATURE, prompt, max_tokens=MAX_TOKENS)
    except KidMeshError as e:
        log.info("done-typing detector unavailable, waiting for the forced summary: %s", e)
        return False
    return parse_verdict(result.text)
