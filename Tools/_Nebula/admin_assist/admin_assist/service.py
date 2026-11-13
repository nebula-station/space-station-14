from __future__ import annotations

import asyncio
import logging
from collections.abc import Awaitable, Callable, Mapping
from datetime import datetime, timedelta, timezone
from uuid import UUID

import asyncpg
import httpx

from .config import Config
from .context import build_context, record_scope
from .db import GameDb
from .delivery import Delivery, SummaryPost
from .detector import finished_typing
from .ingest import AhelpIngest
from .kidmesh import KidMeshError
from .modelwatch import ModelWatch
from .render import summary_system_prompt
from .roster import fetch_round_state
from .store import Store
from .summary import Chat, summarise
from .ticket import TicketMessage
from .tracker import Action, Ticket, Tracker

log = logging.getLogger(__name__)

GAME_VOTE_LOOKBACK = timedelta(days=7)

UNAVAILABLE_ALERT_EVERY = timedelta(minutes=10)
DETECTOR_MAX_MESSAGES = 20
REQUEST_CLOSE = 1
REQUEST_BRIEFING = 2
BRIEFING_REQUEST_MAX_AGE = timedelta(minutes=2)
UNAVAILABLE_TEXT = "Сводки A-Help временно недоступны: модель не отвечает. Тикеты приходят как обычно, читайте их сами."

def utcnow() -> datetime:
    return datetime.now(timezone.utc)


class Service:
    def __init__(
        self,
        config: Config,
        game: GameDb,
        store: Store,
        llm: Chat,
        delivery: Delivery,
        http: httpx.AsyncClient,
        *,
        rules: str,
        watch: ModelWatch | None = None,
        now: Callable[[], datetime] = utcnow,
    ) -> None:
        self._config = config
        self._game = game
        self._store = store
        self._llm = llm
        self._delivery = delivery
        self._http = http
        self._watch = watch
        self._now = now
        self._system = summary_system_prompt(rules)
        self._tracker = Tracker(config)
        self._ahelp: AhelpIngest | None = None
        self._scope: int | None = None
        self._server_id: int | None = None
        self._gate = asyncio.Semaphore(config.max_parallel_calls)
        self._tasks: set[asyncio.Task] = set()
        self._names: dict[UUID, str] = {}
        self._unavailable_alert_at: datetime | None = None
        self._request_cursor = 0
        self._requests_off = False
        self._closed: dict[tuple[UUID, int | None], datetime] = {}


    async def start(self) -> None:
        cfg = self._config
        server_id = None
        if cfg.server_name:
            server_id = await self._game.server_id(cfg.server_name)
            if server_id is None:
                log.warning("no server named %r in the database yet (adminlogs.server_name): reading every server", cfg.server_name)
        self._scope = await record_scope(self._game, cfg)
        self._server_id = server_id
        self._ahelp = AhelpIngest(self._game, self._store, server_id=server_id)
        await self._ahelp.start()
        self._tracker.restore(await self._store.open_tickets(self._now() - timedelta(seconds=cfg.ticket_gap_s), server_id=server_id))
        await self.requests_tick()
        log.info("started: %s open tickets restored, A-Help cursor at %s", len(self._tracker.tickets), self._ahelp.position)

    async def run(self) -> None:
        await self.start()
        cfg = self._config
        loops = [
            self._loop("ahelp", self.ahelp_tick, cfg.poll_s),
            self._loop("feedback", self.feedback_tick, cfg.feedback_poll_s),
        ]
        if self._watch is not None:
            loops.append(self._loop("catalog", self._watch.check, cfg.catalog_poll_s))
        await asyncio.gather(*loops)

    @staticmethod
    async def _loop(name: str, tick: Callable[[], Awaitable[None]], interval: float) -> None:
        while True:
            try:
                await tick()
            except Exception:
                log.exception("%s loop: tick failed", name)
                await asyncio.sleep(max(interval, 5.0))
            else:
                await asyncio.sleep(interval)

    async def drain(self) -> None:
        while self._tasks:
            await asyncio.gather(*list(self._tasks), return_exceptions=True)

    def _spawn(self, coro: Awaitable[None]) -> None:
        task = asyncio.ensure_future(coro)
        self._tasks.add(task)
        task.add_done_callback(self._tasks.discard)


    async def ahelp_tick(self) -> None:
        rows = await self._ahelp.poll()
        if rows:
            changed: dict[int, Ticket] = {}
            for row in rows:
                ticket = self._tracker.observe(row)
                changed[id(ticket)] = ticket
                if not row["sender_is_admin"] and row["sender_user_id"] == row["ticket_owner_user_id"]:
                    self._names[row["ticket_owner_user_id"]] = row["sender_name"]
            await self._store.save_tickets(changed.values(), cursor=(self._ahelp.cursor, self._ahelp.position))
        await self.requests_tick()

        for action in self._tracker.due(self._now()):
            if action.ticket.busy:
                continue
            action.ticket.busy = True
            self._spawn(self._act(action))

    async def requests_tick(self) -> None:
        if self._requests_off:
            return
        now = self._now()
        since = now - timedelta(seconds=self._config.ticket_gap_s)
        try:
            rows = await self._game.ticket_requests_after(self._request_cursor, since, server_id=self._server_id)
        except asyncpg.UndefinedTableError:
            log.warning("the game has no ahelp_ticket_request table (an older game build): closing tickets and briefings on request do nothing")
            self._requests_off = True
            return
        for key, at in list(self._closed.items()):
            if at < since:
                del self._closed[key]
        for row in rows:
            self._request_cursor = row["ahelp_ticket_request_id"]
            owner, round_id, at = row["ticket_owner_user_id"], row["round_id"], row["requested_at"]
            if row["kind"] == REQUEST_CLOSE:
                await self._close(owner, round_id, at)
            elif row["kind"] == REQUEST_BRIEFING and now - at <= BRIEFING_REQUEST_MAX_AGE:
                await self._brief_now(owner, round_id)

    async def _close(self, owner: UUID, round_id: int | None, at: datetime) -> None:
        self._closed[(owner, round_id)] = at
        ticket = self._tracker.close(owner, round_id)
        if ticket is None or ticket.last_player_message_at is None or ticket.last_player_message_at <= at:
            return
        new = self._tracker.rebuild(owner, round_id, ticket.server_id, await self._game.ahelp_messages(owner, round_id, since=at))
        if new is not None:
            await self._store.save_tickets([new])

    async def _brief_now(self, owner: UUID, round_id: int | None) -> None:
        ticket = self._tracker.get(owner, round_id)
        if ticket is not None and ticket.briefing:
            log.info("ticket %s: briefing asked for by hand while one is being made", ticket.ticket_id)
            return
        if ticket is not None and ticket.summaries > 0:
            log.info("ticket %s: briefing asked for by hand, but the ticket already has one", ticket.ticket_id)
            return
        if ticket is None:
            ticket = self._tracker.rebuild(owner, round_id, self._server_id, await self._game.ahelp_messages(owner, round_id))
            if ticket is None or ticket.last_player_message_id is None:
                log.info("briefing asked for by hand, but %s has no A-Help messages from the player in round %s", owner, round_id)
                if ticket is not None:
                    self._tracker.close(owner, round_id)
                return
            await self._store.save_tickets([ticket])
        ticket.wanted = True

    async def _act(self, action: Action) -> None:
        ticket = action.ticket
        try:
            async with self._gate:
                if action.kind == "detect":
                    ticket.detector_calls += 1
                    await self._detect(ticket)
                elif action.kind == "summarise":
                    ticket.briefing, ticket.wanted = True, False
                    await self._mark_started(ticket, action.trigger or "forced")
                    await self._summarise(ticket, action.trigger or "forced")
                elif action.kind == "give_up":
                    await self._give_up(ticket, action.trigger or "")
        except Exception as e:
            if isinstance(e, KidMeshError):
                log.warning("ticket %s: %s failed: %s", ticket.ticket_id, action.kind, e)
            else:
                log.exception("ticket %s: %s failed", ticket.ticket_id, action.kind)
            if action.kind == "summarise":
                Tracker.failed(ticket, self._now())
                await self._alert_unavailable()
            elif action.kind == "detect":
                ticket.detector_verdict = (ticket.last_player_message_id, False)
        finally:
            ticket.busy = ticket.briefing = False

    async def _mark_started(self, ticket: Ticket, trigger: str) -> None:
        if ticket.ticket_id is None:
            return
        try:
            await self._store.mark_briefing_started(ticket, trigger)
        except asyncpg.PostgresError as e:
            log.warning("ticket %s: could not mark the briefing as started: %s", ticket.ticket_id, e)

    async def _thread(self, ticket: Ticket) -> list[Mapping]:
        return await self._game.ahelp_messages(ticket.owner, ticket.round_id, since=ticket.opened_at)

    async def _detect(self, ticket: Ticket) -> None:
        rows = [r for r in await self._thread(ticket) if not r["sender_is_admin"]]
        if not rows:
            return
        last = rows[-1]
        messages = [TicketMessage(f"{r['sent_at']:%H:%M:%S}", r["sender_name"], r["text"]) for r in rows[-DETECTOR_MAX_MESSAGES:]]
        quiet = (self._now() - last["sent_at"]).total_seconds()
        done = await finished_typing(self._llm, messages, quiet)
        ticket.detector_verdict = (last["ahelp_message_id"], done)

    async def _summarise(self, ticket: Ticket, trigger: str) -> None:
        rows = await self._thread(ticket)
        player_ids = [r["ahelp_message_id"] for r in rows if not r["sender_is_admin"]]
        if not player_ids:
            ticket.handled_through_id = ticket.last_player_message_id
            return
        roster = await fetch_round_state(self._config, self._http)
        ctx = await build_context(
            self._game,
            self._config,
            owner=ticket.owner,
            round_id=ticket.round_id,
            messages=rows,
            roster=roster,
            scope_server_id=self._scope,
        )
        summary = await summarise(self._llm, self._system, ctx, max_tokens=self._config.summary_max_tokens)
        closed = self._closed.get((ticket.owner, ticket.round_id))
        if trigger != "manual" and closed is not None and ticket.last_player_message_at <= closed:
            log.info("ticket %s: closed while its briefing was being made, the briefing is dropped", ticket.ticket_id)
            return

        through = max(player_ids)
        ticket.handled_through_id = through
        ticket.summaries += 1
        ticket.priority = summary.priority
        ticket.failures, ticket.retry_at, ticket.detector_calls = 0, None, 0
        participants = [p.user_id for p in ctx.participants if p.user_id is not None]
        summary_id = await self._store.add_summary(ticket, summary, trigger, through, participants)
        log.info(
            "ticket %s: briefing %s (%s, %s attempt(s), %s, ~%.4f ₽)%s",
            ticket.ticket_id, summary_id, trigger, summary.attempts, summary.model, summary.cost_rub,
            " UNRELIABLE: " + "; ".join(summary.problems) if summary.unreliable else "",
        )  # fmt: skip

        post = SummaryPost(await self._owner_name(ticket), ctx.round.number, summary, ctx.messages, ticket.summaries)
        ref = await self._delivery.summary(post)
        if ref is not None:
            await self._store.mark_delivered(summary_id, ref)

    async def _give_up(self, ticket: Ticket, reason: str) -> None:
        log.warning("ticket %s: no briefing (%s)", ticket.ticket_id, reason)
        ticket.handled_through_id = ticket.last_player_message_id
        ticket.failures, ticket.retry_at, ticket.detector_calls = 0, None, 0
        await self._store.save_tickets([ticket])

    async def _alert_unavailable(self) -> None:
        now = self._now()
        if self._unavailable_alert_at is None or now - self._unavailable_alert_at >= UNAVAILABLE_ALERT_EVERY:
            self._unavailable_alert_at = now
            await self._delivery.alert(UNAVAILABLE_TEXT)

    async def _owner_name(self, ticket: Ticket) -> str:
        if ticket.owner not in self._names:
            known = await self._game.players([ticket.owner])
            self._names[ticket.owner] = known[ticket.owner]["last_seen_user_name"] if ticket.owner in known else str(ticket.owner)
        return self._names[ticket.owner]


    async def feedback_tick(self) -> None:
        game_votes = await self._game.briefing_votes_since(self._now() - GAME_VOTE_LOOKBACK)
        if added := await self._store.add_game_votes(game_votes):
            log.info("feedback: %s new in-game vote(s)", added)
