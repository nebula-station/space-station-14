from __future__ import annotations

import re

import pytest

from admin_assist.detector import finished_typing, parse_verdict
from admin_assist.kidmesh import KidMeshError
from admin_assist.summary import problems_in, summarise
from admin_assist.ticket import LogLine, Participant, RoundInfo, TicketContext, TicketMessage

from .fakes import BRIEFING, FakeLLM

CTX = TicketContext(
    round=RoundInfo(412, "00:31"),
    messages=(TicketMessage("00:30:10", "vasya", "меня убил petya ни за что"),),
    participants=(Participant("vasya", True), Participant("petya")),
    logs=(LogLine("00:29:50", "petya hit vasya", (901,)), LogLine("00:29:55", "vasya state changed to Critical", (902, 903))),
    round_id=77,
)
MADE_UP = BRIEFING.replace("[L2]", "[L2] и добил [L9]")


def test_problems_in():
    assert problems_in(BRIEFING, CTX) == []
    assert problems_in(MADE_UP, CTX) == ["ссылки на логи, которых нет: L9"]
    assert problems_in("Привет! Чем могу помочь?", CTX) == ["нет строки приоритета"]
    assert problems_in(BRIEFING.replace("ОБЫЧНЫЙ", "СРОЧНЫЙ"), CTX) == ["нет строки приоритета"]
    for spelling in ("[Л9]", "[l9]", "[L1-9]", "[L1, L9]", "[Ｌ9]", "Л9"):
        assert problems_in(BRIEFING.replace("[L2]", spelling), CTX) == ["ссылки на логи, которых нет: " + ", ".join(f"L{n}" for n in ((3, 4, 5, 6, 7, 8, 9) if "-" in spelling else (9,)))], spelling
    assert problems_in(BRIEFING.replace("[L2]", "[L-2]"), CTX) == ["ссылки на логи, которые не удалось прочитать: [L-2]"]
    assert problems_in(BRIEFING.replace("[L2]", "[L02] [Л1]"), CTX) == []


async def test_good_briefing_takes_one_call():
    llm = FakeLLM(ahelp_summary=["```\n" + BRIEFING.replace("Приоритет:", "**Приоритет:**") + "\n```"])
    s = await summarise(llm, "SYSTEM", CTX, max_tokens=4000)
    assert s.text == BRIEFING
    assert (s.priority, s.kind, s.unreliable, s.problems, s.attempts) == ("ОБЫЧНЫЙ", "жалоба на игрока", False, (), 1)
    assert s.citations == {"L1": (901,), "L2": (902, 903)}
    assert s.cost_rub == pytest.approx(0.001)
    assert re.match(r"SYSTEM\n\n<round_[0-9a-f]{8}>\n", llm.calls[0][1])


async def test_made_up_citation_is_regenerated_once_with_a_fresh_nonce():
    llm = FakeLLM(ahelp_summary=[MADE_UP, BRIEFING])
    s = await summarise(llm, "SYSTEM", CTX)
    assert (s.text, s.unreliable, s.attempts) == (BRIEFING, False, 2)
    assert s.cost_rub == pytest.approx(0.002)
    nonces = [re.search(r"<ticket_(\w+)>", prompt).group(1) for _, prompt in llm.calls]
    assert len(set(nonces)) == 2


async def test_still_bad_after_regeneration_is_delivered_marked_unreliable():
    llm = FakeLLM(ahelp_summary=[MADE_UP, MADE_UP.replace("[L9]", "[L9] [L14]")])
    s = await summarise(llm, "SYSTEM", CTX)
    assert llm.count("ahelp_summary") == 2
    assert s.unreliable and s.attempts == 2
    assert s.text == MADE_UP
    assert s.problems == ("ссылки на логи, которых нет: L9",)
    assert set(s.citations) == {"L1", "L2"}


async def test_no_priority_line_is_unreliable_and_has_no_priority():
    llm = FakeLLM(ahelp_summary=["Привет! Чем могу помочь?"])
    s = await summarise(llm, "SYSTEM", CTX)
    assert (s.priority, s.unreliable, s.attempts) == (None, True, 2)


async def test_failed_regeneration_keeps_the_first_answer():
    llm = FakeLLM(ahelp_summary=[MADE_UP, KidMeshError("down")])
    s = await summarise(llm, "SYSTEM", CTX)
    assert (s.text, s.unreliable, s.attempts) == (MADE_UP, True, 1)


async def test_model_down_raises():
    with pytest.raises(KidMeshError):
        await summarise(FakeLLM(ahelp_summary=[KidMeshError("down")]), "SYSTEM", CTX)


@pytest.mark.parametrize(
    ("answer", "done"),
    [("DONE", True), ("done.", True), ("**DONE**", True), ("WAIT", False), ("", False), ("Я думаю, что игрок закончил", False), ("WAIT, not DONE", False)],
)
def test_detector_verdict(answer, done):
    assert parse_verdict(answer) is done


async def test_detector_asks_about_player_lines_only_and_reads_failure_as_wait():
    messages = [TicketMessage("00:10:00", "vasya", "админ"), TicketMessage("00:10:02", "Boss", "да?", is_admin=True)]
    llm = FakeLLM(done_typing=["DONE"])
    assert await finished_typing(llm, messages, 7.2) is True
    prompt = llm.calls[0][1]
    assert "<seconds_since_last>7</seconds_since_last>" in prompt and "vasya: админ" in prompt and "Boss" not in prompt
    assert await finished_typing(FakeLLM(done_typing=[KidMeshError("down")]), messages, 7) is False


async def test_the_attempt_with_fewer_bad_citations_wins_not_the_one_with_fewer_kinds_of_problem():
    many = MADE_UP.replace("[L9]", "[L9] [L10] [L11]")
    two_kinds = MADE_UP.replace("ОБЫЧНЫЙ", "СРОЧНЫЙ")
    s = await summarise(FakeLLM(ahelp_summary=[many, two_kinds]), "SYSTEM", CTX)
    assert s.text == two_kinds and len(s.problems) == 2


async def test_a_logs_line_that_cites_nothing_is_marked():
    def logs_line(text):
        return re.sub(r"(?m)^Логи: .*$", "Логи: " + text, BRIEFING)

    async def uncited(text):
        return (await summarise(FakeLLM(ahelp_summary=[text]), "SYSTEM", CTX)).uncited

    assert not await uncited(BRIEFING)
    assert await uncited(logs_line("petya трижды ударил vasya ломом"))
    assert not await uncited(logs_line("в логах подтверждений нет"))
    no_logs = FakeLLM(ahelp_summary=[logs_line("petya ударил vasya")])
    from dataclasses import replace

    assert not (await summarise(no_logs, "SYSTEM", replace(CTX, logs=()))).uncited


def test_em_dashes_come_out_of_the_models_words_but_not_out_of_quotes():
    from admin_assist.briefing import tidy

    text = tidy(
        "Оценка: скорее нарушение — нападение без причины; уверенность: высокая\n"
        'Правила: п. 1.1: "Убийства — без основания запрещены" — применимо\n'
        "Проверить: чат за 00:00—00:42, [L3—L5]"
    )
    assert text == (
        "Оценка: скорее нарушение, нападение без причины; уверенность: высокая\n"
        'Правила: п. 1.1: "Убийства — без основания запрещены", применимо\n'
        "Проверить: чат за 00:00-00:42, [L3-L5]"
    )


def test_typographic_quotes_and_ellipsis_become_plain():
    from admin_assist.briefing import tidy

    text = tidy("Суть: игрок пишет «меня убили — просто так…», а в правилах „без причины“ и “так”, ‘да’")
    assert text == "Суть: игрок пишет \"меня убили — просто так...\", а в правилах \"без причины\" и \"так\", 'да'"
