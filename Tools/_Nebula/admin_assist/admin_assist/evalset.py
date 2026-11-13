from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

from .briefing import PRIORITIES, cited_ids, parse_briefing, unknown_citations
from .ticket import STATES, LogLine, Participant, RoundInfo, TicketContext, TicketMessage

EVAL_DIR = Path(__file__).resolve().parent.parent / "evals" / "ahelp"
RULES_FILE = Path(__file__).resolve().parent.parent / "rules" / "nebula.ru.txt"

TYPES = ("жалоба", "вопрос", "баг", "признание", "оспаривание", "другое")
ASSESSMENTS = ("скорее валидно", "скорее нарушение", "неясно")
SHORT_TYPES = ("вопрос", "баг")
MAX_BRIEFING_CHARS = 1200

_EXPECT_KEYS = {"priority", "type", "assessment", "cite_all", "cite_any", "contains", "not_contains"}


@dataclass(frozen=True)
class EvalCase:
    id: str
    title: str
    context: TicketContext
    expect: dict
    review: tuple[str, ...]
    nonce: str | None = None


def context_from_json(data: dict) -> TicketContext:
    return TicketContext(
        round=RoundInfo(**data["round"]),
        messages=tuple(TicketMessage(**m) for m in data["messages"]),
        participants=tuple(
            Participant(**{**p, "antag": None if p.get("antag") is None else tuple(p["antag"])})
            for p in data["participants"]
        ),
        logs=tuple(LogLine(log["time"], log["text"]) for log in data.get("logs", ())),
        window=tuple(data["window"]) if data.get("window") else None,
        history=tuple(data.get("history", ())),
    )


def load_case(path: Path) -> EvalCase:
    data = json.loads(path.read_text(encoding="utf-8"))
    case = EvalCase(
        id=data["id"],
        title=data["title"],
        context=context_from_json(data["context"]),
        expect=data["expect"],
        review=tuple(data.get("review", ())),
        nonce=data.get("nonce"),
    )
    problems = validate_case(case)
    if case.id != path.stem:
        problems.append(f"id {case.id!r} does not match the file name")
    if problems:
        raise ValueError(f"{path.name}: " + "; ".join(problems))
    return case


def load_cases(directory: Path = EVAL_DIR) -> list[EvalCase]:
    return [load_case(p) for p in sorted(directory.glob("*.json"))]


def validate_case(case: EvalCase) -> list[str]:
    problems = []
    ctx, expect = case.context, case.expect
    if not ctx.messages:
        problems.append("no ticket messages")
    if sum(p.is_author for p in ctx.participants) != 1:
        problems.append("exactly one participant must be the author")
    for p in ctx.participants:
        if p.state is not None and p.state not in STATES:
            problems.append(f"unknown state {p.state!r}")
    unknown = set(expect) - _EXPECT_KEYS
    if unknown:
        problems.append(f"unknown expect keys {sorted(unknown)}")
    for key, allowed in (("priority", PRIORITIES), ("type", TYPES), ("assessment", ASSESSMENTS)):
        for value in expect.get(key, ()):
            if value not in allowed:
                problems.append(f"expect.{key}: {value!r} is not one of {allowed}")
    if not expect.get("priority") or not expect.get("type"):
        problems.append("expect.priority and expect.type are required")
    valid = ctx.valid_citations()
    for key in ("cite_all", "cite_any"):
        for cite in expect.get(key, ()):
            if cite not in valid:
                problems.append(f"expect.{key}: {cite} is not a log line of this case")
    for key in ("contains", "not_contains"):
        for field, needles in expect.get(key, {}).items():
            if not isinstance(needles, list) or not all(isinstance(n, str) and n for n in needles):
                problems.append(f"expect.{key}.{field} must be a list of non-empty strings")
    return problems


def check(case: EvalCase, answer: str) -> list[str]:
    failures = []
    fields = parse_briefing(answer)
    expect = case.expect
    lowered = answer.lower()

    for label in ("Приоритет", "Тип", "Суть"):
        if label not in fields:
            failures.append(f"нет строки «{label}:»")
    if not answer.lstrip().startswith("Приоритет:"):
        failures.append("брифинг начинается не с «Приоритет:»")
    if len(answer) > MAX_BRIEFING_CHARS:
        failures.append(f"слишком длинно: {len(answer)} символов")

    priority = fields.get("Приоритет", "")
    if priority not in PRIORITIES:
        failures.append(f"Приоритет: {priority!r}")
    elif priority not in expect["priority"]:
        failures.append(f"Приоритет {priority}, ожидалось {' / '.join(expect['priority'])}")

    kind = fields.get("Тип", "").lower()
    if kind not in TYPES:
        failures.append(f"Тип: {kind!r}")
    elif kind not in expect["type"]:
        failures.append(f"Тип {kind}, ожидалось {' / '.join(expect['type'])}")

    assessment = fields.get("Оценка", "").lower()
    if kind in SHORT_TYPES:
        extra = [f for f in fields if f not in ("Приоритет", "Тип", "Суть", "Правила")]
        if extra:
            failures.append(f"для типа «{kind}» лишние строки: {', '.join(extra)}")
    elif kind in TYPES:
        if not assessment.startswith(ASSESSMENTS):
            failures.append(f"Оценка: {fields.get('Оценка', '')[:40]!r}")
        if "уверенность:" not in assessment:
            failures.append("в Оценке нет уверенности")
    if expect.get("assessment") and not assessment.startswith(tuple(expect["assessment"])):
        failures.append(f"Оценка {assessment[:30]!r}, ожидалось {' / '.join(expect['assessment'])}")

    unknown = unknown_citations(answer, case.context.valid_citations())
    if unknown:
        failures.append(f"цитируются несуществующие логи: {', '.join(unknown)}")
    cited = set(cited_ids(answer))
    missing = [c for c in expect.get("cite_all", ()) if c not in cited]
    if missing:
        failures.append(f"не процитированы: {', '.join(missing)}")
    if expect.get("cite_any") and not cited.intersection(expect["cite_any"]):
        failures.append(f"не процитирован ни один из: {', '.join(expect['cite_any'])}")

    def haystack(field: str) -> str | None:
        return lowered if field == "*" else (fields[field].lower() if field in fields else None)

    for field, needles in expect.get("contains", {}).items():
        text = haystack(field)
        for needle in needles:
            if text is None or needle.lower() not in text:
                failures.append(f"в «{field}» нет «{needle}»")
    for field, needles in expect.get("not_contains", {}).items():
        text = haystack(field) or ""
        for needle in needles:
            if needle.lower() in text:
                failures.append(f"в «{field}» есть «{needle}»")
    return failures
