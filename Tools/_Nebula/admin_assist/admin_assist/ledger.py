from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Protocol

import asyncpg


@dataclass(frozen=True)
class Usage:
    prompt_tokens: int = 0
    cached_tokens: int = 0
    completion_tokens: int = 0


@dataclass(frozen=True)
class LedgerEntry:
    feature: str
    model: str
    usage: Usage
    cost_rub: float
    fell_back: bool


def estimate_cost_rub(usage: Usage, multiplier: float, price_rub_per_mtok: float, cached_input_factor: float) -> float:
    cached = min(usage.cached_tokens, usage.prompt_tokens)
    billable = (usage.prompt_tokens - cached) + cached * cached_input_factor + usage.completion_tokens
    return billable / 1_000_000 * price_rub_per_mtok * multiplier


class Ledger(Protocol):
    async def record(self, entry: LedgerEntry) -> None: ...

    async def month_spend(self) -> float:
        ...

    async def claim_alert(self, level: int) -> bool:
        ...


class MemoryLedger:
    def __init__(self) -> None:
        self.entries: list[LedgerEntry] = []
        self.alerts: set[int] = set()

    async def record(self, entry: LedgerEntry) -> None:
        self.entries.append(entry)

    async def month_spend(self) -> float:
        return sum(e.cost_rub for e in self.entries)

    async def claim_alert(self, level: int) -> bool:
        if level in self.alerts:
            return False
        self.alerts.add(level)
        return True


class PgLedger:
    def __init__(self, pool: asyncpg.Pool) -> None:
        self._pool = pool

    async def record(self, entry: LedgerEntry) -> None:
        await self._pool.execute(
            """
            INSERT INTO admin_assist.llm_ledger
                (feature, model, prompt_tokens, cached_tokens, completion_tokens, cost_rub, fell_back)
            VALUES ($1, $2, $3, $4, $5, $6, $7)
            """,
            entry.feature,
            entry.model,
            entry.usage.prompt_tokens,
            entry.usage.cached_tokens,
            entry.usage.completion_tokens,
            entry.cost_rub,
            entry.fell_back,
        )

    async def month_spend(self) -> float:
        value = await self._pool.fetchval(
            """
            SELECT coalesce(sum(cost_rub), 0)::float8 FROM admin_assist.llm_ledger
            WHERE at >= date_trunc('month', now() AT TIME ZONE 'UTC') AT TIME ZONE 'UTC'
            """
        )
        return float(value)

    async def claim_alert(self, level: int) -> bool:
        month = datetime.now(timezone.utc).date().replace(day=1)
        row = await self._pool.fetchrow(
            """
            INSERT INTO admin_assist.budget_alert (month, level) VALUES ($1, $2)
            ON CONFLICT DO NOTHING RETURNING level
            """,
            month,
            level,
        )
        return row is not None
