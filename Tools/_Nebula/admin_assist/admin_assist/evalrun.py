from __future__ import annotations

import argparse
import asyncio
import json
import time
from datetime import datetime, timezone
from pathlib import Path

from .config import load_config, read_cvars
from .evalset import EVAL_DIR, RULES_FILE, EvalCase, check, load_cases
from .kidmesh import KidMesh, KidMeshError
from .ledger import MemoryLedger
from .render import SUMMARY_PROMPT_VERSION, summary_system_prompt
from .summary import summarise

RESULTS_DIR = EVAL_DIR.parent / "results"


async def run_case(llm: KidMesh, system: str, case: EvalCase, max_tokens: int) -> dict:
    started = time.monotonic()
    try:
        summary = await summarise(llm, system, case.context, max_tokens=max_tokens)
    except KidMeshError as e:
        return {"id": case.id, "passed": False, "failures": [f"модель не ответила: {e}"], "seconds": time.monotonic() - started}
    return {
        "id": case.id,
        "passed": not (failures := check(case, summary.text)),
        "failures": failures,
        "unreliable": summary.unreliable,
        "problems": list(summary.problems),
        "attempts": summary.attempts,
        "model": summary.model,
        "cost_rub": summary.cost_rub,
        "seconds": time.monotonic() - started,
        "chars": len(summary.text),
        "answer": summary.text,
        "review": list(case.review),
    }


async def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(prog="admin_assist.evalrun", description="Run the A-Help eval set against KIDMesh.")
    parser.add_argument("only", nargs="*", help="run only cases whose id contains one of these")
    parser.add_argument("--rules", type=Path, default=RULES_FILE, help="rulebook file (default: the Nebula rules)")
    parser.add_argument("--prompt-version", type=int, default=SUMMARY_PROMPT_VERSION)
    parser.add_argument("--concurrency", type=int, default=4)
    parser.add_argument("--config", required=True, help="the game server's TOML config with the admin_assist.* cvars")
    args = parser.parse_args(argv)

    config = load_config(read_cvars(args.config))
    cases = [c for c in load_cases() if not args.only or any(word in c.id for word in args.only)]
    if not cases:
        print("no such cases")
        return 2
    system = summary_system_prompt(args.rules.read_text(encoding="utf-8"), args.prompt_version)
    ledger = MemoryLedger()
    llm = KidMesh(config, ledger)
    gate = asyncio.Semaphore(args.concurrency)

    async def gated(case: EvalCase) -> dict:
        async with gate:
            result = await run_case(llm, system, case, config.summary_max_tokens)
        mark = "ok  " if result["passed"] else "FAIL"
        print(f"{mark} {case.id}  {result['seconds']:.0f}s" + "".join(f"\n       {f}" for f in result["failures"]), flush=True)
        return result

    try:
        results = await asyncio.gather(*(gated(c) for c in cases))
    finally:
        await llm.aclose()

    passed = sum(r["passed"] for r in results)
    spend = await ledger.month_spend()
    prompt = sum(e.usage.prompt_tokens for e in ledger.entries)
    cached = sum(e.usage.cached_tokens for e in ledger.entries)
    seconds = sorted(r["seconds"] for r in results)
    print(
        f"\n{passed} of {len(results)} passed · {len(ledger.entries)} model calls · ~{spend:.3f} ₽ · "
        f"prompt tokens {prompt}, cached {cached} · median {seconds[len(seconds) // 2]:.0f}s, slowest {seconds[-1]:.0f}s"
    )

    RESULTS_DIR.mkdir(exist_ok=True)
    out = RESULTS_DIR / f"{datetime.now(timezone.utc):%Y%m%d-%H%M%S}.json"
    out.write_text(
        json.dumps(
            {"rules": args.rules.name, "prompt_version": args.prompt_version, "models": {t: m.id for t, m in config.models.items()}, "passed": passed,
             "total": len(results), "spend_rub": spend, "results": results},
            ensure_ascii=False,
            indent=2,
        ),
        encoding="utf-8",
    )
    print(f"answers: {out}")
    return 0 if passed == len(results) else 1


if __name__ == "__main__":
    raise SystemExit(asyncio.run(main()))
