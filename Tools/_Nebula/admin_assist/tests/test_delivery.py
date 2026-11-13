from __future__ import annotations

import logging

from admin_assist.delivery import LogDelivery, SummaryPost, format_summary
from admin_assist.summary import Summary
from admin_assist.ticket import TicketMessage

from .fakes import BRIEFING


def summary(text: str = BRIEFING, *, problems: tuple[str, ...] = ()) -> Summary:
    return Summary(text, "ОБЫЧНЫЙ", "жалоба на игрока", bool(problems), problems, {}, 1, "fake-model", 0.01)


def post(**kwargs) -> SummaryPost:
    messages = (TicketMessage("01:10:41", "vasya", "меня убил petya без причины"),)
    return SummaryPost("vasya", 3, kwargs.pop("summary", summary()), messages, **kwargs)


def test_summary_has_header_briefing_and_the_raw_ticket():
    text = format_summary(post(number=2))
    assert text.startswith("Брифинг #2 по тикету vasya, раунд 3\n")
    assert BRIEFING in text and text.endswith("[01:10:41] vasya: меня убил petya без причины")


def test_unreliable_summary_says_so():
    assert "ненадёжно: ссылки на логи, которых нет: L9" in format_summary(post(summary=summary(problems=("ссылки на логи, которых нет: L9",))))


async def test_log_delivery_logs_everything(caplog):
    caplog.set_level(logging.INFO)
    delivery = LogDelivery()
    assert await delivery.summary(post()) == "log"
    await delivery.alert("модель не отвечает", ping=True)
    assert "Брифинг #1" in caplog.text and "ALERT: модель не отвечает" in caplog.text
