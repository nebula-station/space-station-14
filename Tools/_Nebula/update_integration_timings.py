#!/usr/bin/env python3
"""Refresh integration_test_timings.json from the trx results of recent master runs.

Needs an authenticated gh. Run from the repository root:
    python3 Tools/_Nebula/update_integration_timings.py
"""
from __future__ import annotations

import argparse
import json
import re
import statistics
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from collections import defaultdict
from dataclasses import dataclass, field
from pathlib import Path

from integration_test_shards import (
    DEFAULT_TESTS_ROOT,
    NAMESPACE_PREFIX,
    TIMINGS_PATH,
    ClassTiming,
    discover_source_classes,
    load_timings,
    write_timings,
)

DEFAULT_REPO = "nebula-station/space-station-14"
DEFAULT_WORKFLOW = "build-test-debug.yml"
DEFAULT_RUN_COUNT = 5
# Splitting a class needs the time of its methods, the light ones only take up space.
METHODS_MIN_SECONDS = 30
TRX_NAMESPACE = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
NEW_PAIR_MARKER = "Creating a new pair"
PAIR_TIME_PATTERN = re.compile(r"Retrieving pair \d+ from pool took ([\d.]+) ms")


@dataclass
class RunClass:
    seconds: float = 0
    cases: int = 0
    method_seconds: dict[str, float] = field(default_factory=lambda: defaultdict(float))
    method_cases: dict[str, int] = field(default_factory=lambda: defaultdict(int))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--runs", type=int, default=DEFAULT_RUN_COUNT)
    parser.add_argument("--repo", default=DEFAULT_REPO)
    parser.add_argument("--workflow", default=DEFAULT_WORKFLOW)
    parser.add_argument("--branch", default="master")
    parser.add_argument("--timings", type=Path, default=TIMINGS_PATH)
    args = parser.parse_args()

    runs: list[dict[str, RunClass]] = []
    with tempfile.TemporaryDirectory() as temp:
        for run_id in list_runs(args.repo, args.workflow, args.branch, args.runs):
            trx_files = download_trx(args.repo, run_id, Path(temp) / str(run_id))
            print(f"run {run_id}: {len(trx_files)} shards", file=sys.stderr)
            if trx_files:
                runs.append(collect_run(trx_files))
    if not runs:
        raise SystemExit("No trx results found.")

    old = load_timings(args.timings)
    timings = merge(runs, old, set(discover_source_classes(DEFAULT_TESTS_ROOT)))
    write_timings(args.timings, timings)
    kept = sum(1 for name in timings if not any(name in run for run in runs))
    print(f"{len(timings)} classes, {kept} kept from the old file, {len(set(old) - set(timings))} removed",
          file=sys.stderr)
    return 0


def list_runs(repo: str, workflow: str, branch: str, count: int) -> list[int]:
    """Failed runs count too: a run is failed when any shard is, and the other shards are still fine."""
    output = run_gh(
        "run", "list", "-R", repo, "--workflow", workflow, "--branch", branch, "--event", "push",
        "--status", "completed", "--limit", str(count * 3), "--json", "databaseId,conclusion",
    )
    runs = [run["databaseId"] for run in json.loads(output) if run["conclusion"] in ("success", "failure")]
    return runs[:count]


def download_trx(repo: str, run_id: int, destination: Path) -> list[Path]:
    # Expired artifacts make gh fail, such a run just has nothing to give.
    result = subprocess.run(
        ["gh", "run", "download", str(run_id), "-R", repo, "-p", "integration-test-results-*", "-D", str(destination)],
        capture_output=True, text=True,
    )
    if result.returncode != 0:
        print(f"run {run_id}: {result.stderr.strip()}", file=sys.stderr)
        return []
    return sorted(destination.rglob("*.trx"))


def collect_run(trx_files: list[Path]) -> dict[str, RunClass]:
    classes: dict[str, RunClass] = defaultdict(RunClass)
    for path in trx_files:
        for class_name, method, seconds in read_trx(path):
            entry = classes[class_name]
            entry.seconds += seconds
            entry.cases += 1
            entry.method_seconds[method] += seconds
            entry.method_cases[method] += 1
    return classes


def read_trx(path: Path):
    """(class relative to the namespace prefix, method, seconds) for every test case of a shard."""
    root = ET.parse(path).getroot()
    class_names = {
        test.get("id"): test.find("t:TestMethod", TRX_NAMESPACE).get("className")
        for test in root.iterfind(".//t:UnitTest", TRX_NAMESPACE)
    }
    for result in root.iterfind(".//t:UnitTestResult", TRX_NAMESPACE):
        class_name = class_names.get(result.get("testId"), "")
        duration = result.get("duration")
        if not class_name.startswith(NAMESPACE_PREFIX + ".") or duration is None:
            continue
        yield (
            class_name[len(NAMESPACE_PREFIX) + 1:],
            result.get("testName").split("(")[0],
            max(parse_duration(duration) - new_pair_seconds(result), 0),
        )


def new_pair_seconds(result: ET.Element) -> float:
    """A new pair costs about 40 s and goes to whichever test happens to start first on it."""
    output = result.find(".//t:StdOut", TRX_NAMESPACE)
    if output is None or output.text is None or NEW_PAIR_MARKER not in output.text:
        return 0
    match = PAIR_TIME_PATTERN.search(output.text)
    return float(match.group(1)) / 1000 if match is not None else 0


def parse_duration(duration: str) -> float:
    hours, minutes, seconds = duration.split(":")
    return int(hours) * 3600 + int(minutes) * 60 + float(seconds)


def merge(runs: list[dict[str, RunClass]], old: dict[str, ClassTiming], source: set[str]) -> dict[str, ClassTiming]:
    """Median of the runs that have the class. A class without fresh data, e.g. because its shard
    died every time, keeps the old value. Classes gone from the code are dropped."""
    timings: dict[str, ClassTiming] = {}
    for name in source:
        samples = [run[name] for run in runs if name in run]
        if not samples:
            if name in old:
                timings[name] = old[name]
            continue

        seconds = max(round(statistics.median(sample.seconds for sample in samples)), 1)
        methods: dict[str, tuple[int, int]] = {}
        if seconds >= METHODS_MIN_SECONDS:
            for method in {method for sample in samples for method in sample.method_seconds}:
                method_samples = [sample for sample in samples if method in sample.method_seconds]
                methods[method] = (
                    round(statistics.median(sample.method_seconds[method] for sample in method_samples)),
                    max(sample.method_cases[method] for sample in method_samples),
                )
        timings[name] = ClassTiming(seconds, max(sample.cases for sample in samples), methods)
    return timings


def run_gh(*args: str) -> str:
    return subprocess.run(["gh", *args], check=True, capture_output=True, text=True).stdout


if __name__ == "__main__":
    raise SystemExit(main())
