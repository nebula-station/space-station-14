from __future__ import annotations

import logging
from collections.abc import Mapping, Sequence

from .db import GameDb
from .store import Store

log = logging.getLogger(__name__)

OVERLAP = 200
BATCH = 500
AHELP_CURSOR = "ahelp"


def cursor_name(base: str, server_id: int | None) -> str:
    return base if server_id is None else f"{base}:{server_id}"


class _Window:
    def __init__(self, position: int, seen: Sequence[int] = ()) -> None:
        self.position = position
        self._seen = set(seen)

    @property
    def read_from(self) -> int:
        return max(0, self.position - OVERLAP)

    def take(self, rows: Sequence[Mapping], key: str) -> list[Mapping]:
        fresh = [r for r in rows if r[key] not in self._seen]
        for row in fresh:
            self._seen.add(row[key])
            self.position = max(self.position, row[key])
        floor = self.read_from
        self._seen = {i for i in self._seen if i > floor}
        return fresh


class AhelpIngest:
    def __init__(self, game: GameDb, store: Store, *, server_id: int | None = None) -> None:
        self._game = game
        self._store = store
        self._server_id = server_id
        self.cursor = cursor_name(AHELP_CURSOR, server_id)
        self._window: _Window | None = None

    @property
    def position(self) -> int:
        return self._window.position if self._window else 0

    async def start(self) -> None:
        saved = await self._store.cursor(self.cursor)
        if saved is None:
            position = await self._game.ahelp_max_id()
            await self._store.set_cursor(self.cursor, position)
            log.info("ahelp ingest: first start, beginning after message %s", position)
        else:
            position = saved[0]
        window = _Window(position)
        rows = await self._game.ahelp_messages_after(window.read_from, server_id=self._server_id, limit=OVERLAP)
        self._window = _Window(position, [r["ahelp_message_id"] for r in rows if r["ahelp_message_id"] <= position])

    async def poll(self) -> list[Mapping]:
        if self._window is None:
            await self.start()
        rows = await self._game.ahelp_messages_after(self._window.read_from, server_id=self._server_id, limit=BATCH)
        return self._window.take(rows, "ahelp_message_id")
