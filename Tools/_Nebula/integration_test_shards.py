#!/usr/bin/env python3
"""Split Content.IntegrationTests into GitHub Actions shards of similar duration.

Weights come from integration_test_timings.json (see update_integration_timings.py).
Classes missing from it are estimated by their [Test]/[TestCase] markers.
"""
from __future__ import annotations

import argparse
import json
import re
from dataclasses import dataclass, field
from pathlib import Path

# [Test] and its subclasses like [MarkingTest], but not [TestOf] or [TestFixture].
TEST_MARKER_PATTERN = re.compile(r"\[(?:\w*Test|TestCase|TestCaseSource)\b")
NAMESPACE_PATTERN = re.compile(r"(?m)^\s*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)")
INLINE_ATTRIBUTES_PATTERN = re.compile(r"^\s*(?:\[[^\]\n]+\]\s*)+")
TYPE_DECLARATION_PATTERN = re.compile(
    r"^(\s*)"
    r"(?:(?:public|private|protected|internal|sealed|abstract|static|partial|readonly|ref|unsafe|new|file)\s+)*"
    r"(?:class|struct|record(?:\s+(?:class|struct))?)\s+"
    r"([A-Za-z_][A-Za-z0-9_]*)\b"
)
BLOCK_COMMENT_PATTERN = re.compile(r"/\*.*?\*/", re.S)
EXPLICIT_PATTERN = re.compile(r"\bExplicit\b")
UNIT_NAME_SPLIT_PATTERN = re.compile(r"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")

DEFAULT_TESTS_ROOT = Path("Content.IntegrationTests") / "Tests"
NAMESPACE_PREFIX = "Content.IntegrationTests.Tests"
TIMINGS_PATH = Path(__file__).with_name("integration_test_timings.json")
DEFAULT_SHARD_COUNT = 8
FALLBACK_SECONDS_PER_MARKER = 2
MAX_SHARD_NAME_LENGTH = 96
FQN = "FullyQualifiedName"


@dataclass(frozen=True)
class ClassTiming:
    seconds: int
    cases: int
    # Method name -> (seconds, cases).
    methods: dict[str, tuple[int, int]] = field(default_factory=dict)


@dataclass(frozen=True)
class TestUnit:
    name: str
    weight: int
    cases: int
    filter_expression: str


@dataclass
class Shard:
    index: int
    units: list[TestUnit] = field(default_factory=list)
    weight: int = 0
    cases: int = 0

    def add(self, unit: TestUnit) -> None:
        self.units.append(unit)
        self.weight += unit.weight
        self.cases += unit.cases

    @property
    def id(self) -> str:
        return f"shard-{self.index + 1:02d}"

    def to_matrix_entry(self) -> dict[str, object]:
        sorted_units = sorted(self.units, key=lambda unit: unit.name)
        return {
            "id": self.id,
            "name": build_shard_name(self.units, self.weight),
            "seconds": self.weight,
            "cases": self.cases,
            "unit_count": len(sorted_units),
            "units": ", ".join(unit.name for unit in sorted_units),
            "filter": "|".join(unit.filter_expression for unit in sorted_units),
        }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Generate balanced GitHub Actions shards for Content.IntegrationTests.",
        formatter_class=argparse.ArgumentDefaultsHelpFormatter,
    )
    parser.add_argument("--shards", type=int, default=DEFAULT_SHARD_COUNT)
    parser.add_argument("--tests-root", type=Path, default=DEFAULT_TESTS_ROOT)
    parser.add_argument("--timings", type=Path, default=TIMINGS_PATH)
    parser.add_argument("--pretty", action="store_true")
    args = parser.parse_args()
    if args.shards < 1:
        raise SystemExit("--shards must be at least 1.")

    source = discover_source_classes(args.tests_root)
    estimates = estimate_classes(source, load_timings(args.timings))
    units = build_units(estimates, args.shards)
    if not units:
        raise SystemExit("No integration test classes discovered.")

    shards = balance_units(units, args.shards)
    # Anything the parser missed, like a class that only inherits its tests, still runs somewhere.
    # A filter that selects only [Explicit] tests makes NUnit run them, so it never gets a shard of its own.
    min(shards, key=lambda shard: shard.weight).add(build_remainder_unit(list(estimates)))

    matrix = {"include": [shard.to_matrix_entry() for shard in shards if shard.units]}
    if args.pretty:
        for shard in matrix["include"]:
            print(f"{shard['id']}: ~{shard['seconds']} s, {shard['cases']} cases -> {shard['units']}")
    else:
        print(json.dumps(matrix, separators=(",", ":")))
    return 0


def discover_source_classes(tests_root: Path) -> dict[str, int]:
    """Top level types under the tests root, relative to the namespace prefix, with their test markers."""
    if not tests_root.exists():
        raise SystemExit(f"Tests root does not exist: {tests_root}")

    classes: dict[str, int] = {}
    for file_path in sorted(tests_root.rglob("*.cs")):
        # Commented out tests do not run, the line breaks stay so line numbers do not move.
        text = BLOCK_COMMENT_PATTERN.sub(lambda m: "\n" * m.group(0).count("\n"), file_path.read_text(encoding="utf-8"))
        namespace = read_namespace(text)
        if namespace is None:
            continue
        for type_name, markers in read_top_level_types(text):
            name = f"{namespace}.{type_name}".lstrip(".")
            classes[name] = classes.get(name, 0) + markers
    return classes


def read_namespace(text: str) -> str | None:
    match = NAMESPACE_PATTERN.search(text)
    namespace = match.group(1).rstrip(".") if match is not None else NAMESPACE_PREFIX
    if namespace != NAMESPACE_PREFIX and not namespace.startswith(NAMESPACE_PREFIX + "."):
        return None
    return namespace[len(NAMESPACE_PREFIX) + 1:]


def read_top_level_types(text: str) -> list[tuple[str, int]]:
    """Markers inside nested types count towards the top level type that contains them.
    [Explicit] tests do not run in CI and are not counted."""
    lines = text.splitlines()
    declarations: list[tuple[int, int, str]] = []
    for line_index, line in enumerate(lines):
        match = TYPE_DECLARATION_PATTERN.match(INLINE_ATTRIBUTES_PATTERN.sub("", line))
        if match is not None:
            declarations.append((line_index, len(match.group(1).expandtabs()), match.group(2)))
    if not declarations:
        return []

    top_indent = min(indent for _, indent, _ in declarations)
    top_level = [(line_index, name) for line_index, indent, name in declarations if indent == top_indent]
    # Attributes right above a declaration belong to it, so the previous type ends a few lines early.
    starts = [0] + [line_index - count_leading_attributes(lines, line_index) for line_index, _ in top_level[1:]]
    ends = starts[1:] + [len(lines)]
    types = []
    for (line_index, name), start, end in zip(top_level, starts, ends):
        class_attributes = lines[line_index - count_leading_attributes(lines, line_index):line_index]
        explicit = any(EXPLICIT_PATTERN.search(line) for line in class_attributes)
        types.append((name, 0 if explicit else count_markers(lines[start:end])))
    return types


def count_markers(lines: list[str]) -> int:
    """Markers of the attribute blocks that are not [Explicit]."""
    markers = 0
    block: list[str] = []
    for line in lines + [""]:
        if line.lstrip().startswith("["):
            block.append(line)
            continue
        if not EXPLICIT_PATTERN.search("\n".join(block)):
            markers += len(TEST_MARKER_PATTERN.findall("\n".join(block + [line])))
        block = []
    return markers


def count_leading_attributes(lines: list[str], line_index: int) -> int:
    count = 0
    while line_index - count - 1 >= 0 and lines[line_index - count - 1].lstrip().startswith("["):
        count += 1
    return count


def load_timings(path: Path) -> dict[str, ClassTiming]:
    if not path.exists():
        return {}
    data = json.loads(path.read_text(encoding="utf-8"))
    return {
        name: ClassTiming(
            seconds=entry["seconds"],
            cases=entry["cases"],
            methods={method: tuple(value) for method, value in entry.get("methods", {}).items()},
        )
        for name, entry in data.items()
    }


def write_timings(path: Path, timings: dict[str, ClassTiming]) -> None:
    # One class per line keeps the diff of an update readable.
    lines = []
    for name in sorted(timings):
        timing = timings[name]
        entry: dict[str, object] = {"seconds": timing.seconds, "cases": timing.cases}
        if timing.methods:
            entry["methods"] = dict(sorted(timing.methods.items()))
        lines.append(f"  {json.dumps(name)}: {json.dumps(entry)}")
    path.write_text("{\n" + ",\n".join(lines) + "\n}\n", encoding="utf-8")


def estimate_classes(source: dict[str, int], timings: dict[str, ClassTiming]) -> dict[str, ClassTiming]:
    """Classes with known timings, plus new classes with test markers."""
    estimates: dict[str, ClassTiming] = {}
    for name, markers in source.items():
        if name in timings:
            estimates[name] = timings[name]
        elif markers > 0:
            estimates[name] = ClassTiming(seconds=markers * FALLBACK_SECONDS_PER_MARKER, cases=markers)
    return estimates


def build_units(estimates: dict[str, ClassTiming], shard_count: int) -> list[TestUnit]:
    """One unit per class. A class heavier than half of a shard is split into its methods."""
    split_threshold = sum(timing.seconds for timing in estimates.values()) / shard_count / 2
    units: list[TestUnit] = []
    for name, timing in estimates.items():
        nested = [other for other in estimates if other.startswith(name + ".")]
        if timing.methods and timing.seconds > split_threshold:
            units.extend(build_method_units(name, timing, nested))
        else:
            units.append(TestUnit(name, max(timing.seconds, 1), timing.cases, class_filter(name, nested)))
    return sorted(units, key=lambda unit: (-unit.weight, unit.name))


def build_method_units(name: str, timing: ClassTiming, nested: list[str]) -> list[TestUnit]:
    # The exact match takes every [TestCase] of a method: their arguments are not part of the name.
    units = [
        TestUnit(f"{name}.{method}", max(seconds, 1), cases, f"{FQN}={full_name(name)}.{method}")
        for method, (seconds, cases) in timing.methods.items()
    ]
    # Methods added after the last timings update.
    excluded = "&".join(f"{FQN}!={full_name(name)}.{method}" for method in timing.methods)
    rest_seconds = timing.seconds - sum(seconds for seconds, _ in timing.methods.values())
    rest_cases = timing.cases - sum(cases for _, cases in timing.methods.values())
    units.append(TestUnit(f"{name} (rest)", max(rest_seconds, 1), max(rest_cases, 0),
                          f"({class_filter(name, nested)}&{excluded})"))
    return units


def class_filter(name: str, nested: list[str]) -> str:
    """The trailing dot keeps Foo from matching FooBar. A namespace named like the class is excluded."""
    expression = f"{FQN}~{full_name(name)}."
    if not nested:
        return expression
    return "(" + "&".join([expression] + [f"{FQN}!~{full_name(other)}." for other in nested]) + ")"


def build_remainder_unit(class_names: list[str]) -> TestUnit:
    covered = "&".join(f"{FQN}!~{full_name(name)}." for name in sorted(class_names))
    return TestUnit("unlisted", 0, 0, f"({covered})")


def full_name(name: str) -> str:
    return f"{NAMESPACE_PREFIX}.{name}"


def build_shard_name(units: list[TestUnit], weight: int) -> str:
    display_units = [humanize_unit_name(unit.name) for unit in sorted(units, key=lambda u: (-u.weight, u.name))]
    suffix = f" (~{weight} s)"
    return summarize_display_units([unit for unit in display_units if unit], MAX_SHARD_NAME_LENGTH - len(suffix)) + suffix


def humanize_unit_name(unit_name: str) -> str:
    if unit_name == "unlisted":
        return ""
    unit_name = unit_name.split(" ")[0].split(".")[-1]
    for suffix in ("Tests", "Test"):
        if unit_name.endswith(suffix):
            unit_name = unit_name[: -len(suffix)]
            break
    return UNIT_NAME_SPLIT_PATTERN.sub(" ", unit_name).strip()


def summarize_display_units(display_units: list[str], max_length: int) -> str:
    if not display_units:
        return "Unknown"

    selected: list[str] = []
    for name in display_units:
        candidate_units = selected + [name]
        remaining = len(display_units) - len(candidate_units)
        candidate = ", ".join(candidate_units)
        if remaining > 0:
            candidate = f"{candidate} +{remaining} more"
        if selected and len(candidate) > max_length:
            break
        selected.append(name)

    remaining = len(display_units) - len(selected)
    summary = ", ".join(selected)
    if remaining > 0:
        summary = f"{summary} +{remaining} more"
    return summary


def balance_units(units: list[TestUnit], shard_count: int) -> list[Shard]:
    shards = [Shard(index=index) for index in range(min(shard_count, len(units)))]
    for unit in units:
        shard = min(shards, key=lambda candidate: (candidate.weight, len(candidate.units), candidate.index))
        shard.add(unit)
    return shards


if __name__ == "__main__":
    raise SystemExit(main())
