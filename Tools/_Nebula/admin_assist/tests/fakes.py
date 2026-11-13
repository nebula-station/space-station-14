from __future__ import annotations

import re

from admin_assist.delivery import SummaryPost
from admin_assist.kidmesh import ChatResult, KidMeshError
from admin_assist.ledger import Usage
from admin_assist.render import reminder

BRIEFING = """Приоритет: ОБЫЧНЫЙ
Тип: жалоба на игрока
Суть: игрок пишет, что его убили без причины.
Участники: vasya (автор), petya
Логи: petya ударил vasya [L1], vasya упал в крит [L2].
Расхождения: нет
Правила: 3.1 Убийство без причины
Оценка: скорее нарушение
Проверить: спросить petya о причине
Флаги: нет"""


def nonced(text: str, nonce: str) -> str:
    text = re.sub(r"<(/?)(round|context|history)>", rf"<\1\2_{nonce}>", text)
    return text + "\n\n" + reminder(nonce)


class FakeLLM:
    def __init__(self, **scripts) -> None:
        self.scripts = {feature: list(items) if isinstance(items, list) else [items] for feature, items in scripts.items()}
        self.calls: list[tuple[str, str]] = []

    def count(self, feature: str) -> int:
        return sum(1 for f, _ in self.calls if f == feature)

    async def chat(self, feature, messages, *, max_tokens=None, temperature=None) -> ChatResult:
        prompt = "\n\n".join(m["content"] for m in messages)
        self.calls.append((feature, prompt))
        script = self.scripts.get(feature)
        if not script:
            raise KidMeshError(f"{feature}: no model in this test")
        item = script.pop(0) if len(script) > 1 else script[0]
        if callable(item):
            item = item(prompt)
        if isinstance(item, Exception):
            raise item
        return ChatResult(item, None, "fake-model", Usage(100, 0, 20), 0.001, False)


class FakeDelivery:
    def __init__(self) -> None:
        self.summaries: list[SummaryPost] = []
        self.alerts: list[tuple[str, bool]] = []
        self.fail_summaries = False

    async def summary(self, post: SummaryPost) -> str | None:
        if self.fail_summaries:
            return None
        self.summaries.append(post)
        return f"msg-{len(self.summaries)}"

    async def alert(self, text: str, *, ping: bool = False) -> None:
        self.alerts.append((text, ping))
