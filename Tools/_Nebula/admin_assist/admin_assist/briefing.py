from __future__ import annotations

import re
import unicodedata

FIELDS = ("Приоритет", "Тип", "Суть", "Участники", "Логи", "Расхождения", "Правила", "Оценка", "Проверить", "Флаги")
PRIORITIES = ("ВЫСОКИЙ", "ОБЫЧНЫЙ", "НИЗКИЙ")

_FIELD_LINE = re.compile(rf"^({'|'.join(FIELDS)}):[ \t]*(.*)$")
_FENCE = re.compile(r"^```\w*\n|\n```$")

_BRACKETS = re.compile(r"\[([^\[\]\n]{1,80})\]")
_L = "LlЛл"
_LOOKS_CITED = re.compile(rf"(?<![^\W_])[{_L}]\s?[-–—]?\s?\d")
_ONE = re.compile(rf"[{_L}](\d{{1,6}})")
_RANGE = re.compile(rf"[{_L}](\d{{1,6}})\s*[-–—]\s*[{_L}]?(\d{{1,6}})")
_BARE = re.compile(r"(?<![^\W_])[LЛ](\d{1,6})\b")
MAX_RANGE = 30


def tidy(text: str) -> str:
    text = text.strip()
    text = _FENCE.sub("", text).strip()
    return plain_dashes(plain_quotes(text.replace("**", "")))


_PLAIN = str.maketrans({"«": '"', "»": '"', "„": '"', "“": '"', "”": '"', "‘": "'", "’": "'"})


def plain_quotes(text: str) -> str:
    return text.translate(_PLAIN).replace("…", "...")


_QUOTED = re.compile(r"\"[^\"\n]*\"")
_SPACED_DASH = re.compile(r"[ \t]*,?[ \t]+—[ \t]+")


def plain_dashes(text: str) -> str:
    parts = []
    last = 0
    for quote in _QUOTED.finditer(text):
        parts.append(_unquoted_dashes(text[last:quote.start()]))
        parts.append(quote.group(0))
        last = quote.end()
    parts.append(_unquoted_dashes(text[last:]))
    return "".join(parts)


def _unquoted_dashes(text: str) -> str:
    text = _SPACED_DASH.sub(", ", text)
    text = re.sub(r"(?m)^([ \t]*)—[ \t]*", r"\1- ", text)
    return text.replace("—", "-")


def parse_briefing(text: str) -> dict[str, str]:
    fields: dict[str, str] = {}
    current = None
    for line in text.strip().splitlines():
        match = _FIELD_LINE.match(line.strip())
        if match and match.group(1) not in fields:
            current = match.group(1)
            fields[current] = match.group(2).strip()
        elif current is not None:
            fields[current] = (fields[current] + "\n" + line.strip()).strip()
    return fields


def read_citations(text: str) -> tuple[list[str], list[str]]:
    text = unicodedata.normalize("NFKC", text)
    found: list[tuple[int, str]] = []
    malformed: list[str] = []
    for group in _BRACKETS.finditer(text):
        inner = group.group(1)
        if not _LOOKS_CITED.search(inner):
            continue
        for part in re.split(r"[,;]|\sи\s", inner):
            part = part.strip()
            if one := _ONE.fullmatch(part):
                found.append((group.start(), f"L{int(one.group(1))}"))
            elif (span := _RANGE.fullmatch(part)) and 0 <= int(span.group(2)) - int(span.group(1)) <= MAX_RANGE:
                found.extend((group.start(), f"L{n}") for n in range(int(span.group(1)), int(span.group(2)) + 1))
            else:
                malformed.append(group.group(0))
                break
    outside = _BRACKETS.sub(lambda m: " " * len(m.group(0)), text)
    found.extend((m.start(), f"L{int(m.group(1))}") for m in _BARE.finditer(outside))
    found.sort(key=lambda pair: pair[0])
    return list(dict.fromkeys(c for _, c in found)), list(dict.fromkeys(malformed))


def cited_ids(text: str) -> list[str]:
    return read_citations(text)[0]


def unknown_citations(text: str, valid: frozenset[str] | set[str]) -> list[str]:
    return [c for c in cited_ids(text) if c not in valid]


def malformed_citations(text: str) -> list[str]:
    return read_citations(text)[1]
