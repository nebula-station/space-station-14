from __future__ import annotations

import argparse
import asyncio
import logging
import os
import signal
import sys
import threading
from collections.abc import Callable
from pathlib import Path
from typing import BinaryIO

import httpx

from .config import Config, ConfigError, load_config, read_cvars, read_stdin_cvars
from .db import GameDb, own_pool
from .delivery import LogDelivery
from .kidmesh import KidMesh
from .ledger import PgLedger
from .modelwatch import ModelWatch
from .service import Service
from .store import Store

log = logging.getLogger("admin_assist")


def read_rules(config: Config) -> str:
    if not config.rules_file:
        raise ConfigError("admin_assist.rules_file is not set: the summary prompt needs the server rules")
    path = Path(config.rules_file)
    try:
        rules = path.read_text(encoding="utf-8").strip()
    except OSError as e:
        raise ConfigError(f"admin_assist.rules_file={config.rules_file!r} cannot be read: {e}") from None
    if not rules:
        raise ConfigError(f"admin_assist.rules_file={config.rules_file!r} is empty")
    return rules


def check(config: Config) -> None:
    for name, value in (("admin_assist.game_dsn", config.game_dsn), ("admin_assist.own_dsn", config.own_dsn)):
        if not value:
            raise ConfigError(f"{name} is not set")
    if not config.models["main"].api_key:
        raise ConfigError("no KIDMesh key: set admin_assist.kidmesh_key (or admin_assist.model_main_key)")


def watch_stdin(stream: BinaryIO, loop: asyncio.AbstractEventLoop, stop: Callable[[], object]) -> threading.Thread:
    fd = stream.fileno()

    def wait() -> None:
        while os.read(fd, 65536):
            pass
        log.info("the game server closed stdin")
        loop.call_soon_threadsafe(stop)

    thread = threading.Thread(target=wait, name="stdin-watch", daemon=True)
    thread.start()
    return thread


async def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(prog="admin_assist", description="Nebula Admin Assist sidecar.")
    source = parser.add_mutually_exclusive_group(required=True)
    source.add_argument("--config", help="the game server's TOML config with the admin_assist.* cvars")
    source.add_argument(
        "--config-stdin",
        action="store_true",
        help="read the config from stdin as sent by the game server (length line, then TOML) and stop once stdin closes",
    )
    args = parser.parse_args(argv)

    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s: %(message)s")
    logging.getLogger("httpx").setLevel(logging.WARNING)
    cvars = read_stdin_cvars(sys.stdin.buffer) if args.config_stdin else read_cvars(args.config)
    config = load_config(cvars)
    check(config)
    rules = read_rules(config)

    game = await GameDb.connect(config.game_dsn, config.db_statement_timeout_ms)
    own = await own_pool(config.own_dsn, config.db_statement_timeout_ms)
    http = httpx.AsyncClient(timeout=config.kidmesh_timeout_s)
    try:
        delivery = LogDelivery()
        llm = KidMesh(config, PgLedger(own), http=http, alert=delivery.alert)
        watch = ModelWatch(config, http, llm, delivery.alert)
        service = Service(config, game, Store(own), llm, delivery, http, rules=rules, watch=watch)

        running = asyncio.ensure_future(service.run())
        loop = asyncio.get_running_loop()
        for sig in (signal.SIGINT, signal.SIGTERM):
            try:
                loop.add_signal_handler(sig, running.cancel)
            except NotImplementedError:
                pass
        if args.config_stdin:
            watch_stdin(sys.stdin.buffer, loop, running.cancel)
        try:
            await running
        except asyncio.CancelledError:
            log.info("stopping")
    finally:
        await http.aclose()
        await own.close()
        await game.close()


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except ConfigError as e:
        raise SystemExit(f"admin_assist: {e}") from None
