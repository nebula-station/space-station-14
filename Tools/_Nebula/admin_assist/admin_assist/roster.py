from __future__ import annotations

import logging
from dataclasses import dataclass
from uuid import UUID

import httpx

from .config import Config

log = logging.getLogger(__name__)


@dataclass(frozen=True)
class RosterPlayer:
    user_id: UUID
    username: str
    character: str | None
    job: str | None
    antag: tuple[str, ...]
    state: str
    is_admin: bool


@dataclass(frozen=True)
class RoundState:
    server_name: str
    round_id: int
    round_number: int
    run_level: str
    duration_s: float
    gamemode: str | None
    map_name: str | None
    player_count: int
    players: tuple[RosterPlayer, ...]

    def by_user(self) -> dict[UUID, RosterPlayer]:
        return {p.user_id: p for p in self.players}


def parse_round_state(data: dict) -> RoundState:
    players = tuple(
        RosterPlayer(
            user_id=UUID(p["UserId"]),
            username=p["Username"],
            character=p.get("CharacterName") or None,
            job=p.get("JobName") or p.get("JobId") or None,
            antag=tuple(a.get("Name") or a["Id"] for a in p.get("AntagRoles") or ()),
            state=p["State"],
            is_admin=bool(p.get("IsAdmin")),
        )
        for p in data.get("Players") or ()
    )
    return RoundState(
        server_name=data.get("ServerName") or "",
        round_id=int(data["RoundId"]),
        round_number=int(data["RoundNumber"]),
        run_level=str(data.get("RunLevel") or ""),
        duration_s=float(data.get("RoundDurationSeconds") or 0),
        gamemode=data.get("GamePreset") or None,
        map_name=data.get("MapName") or None,
        player_count=int(data.get("PlayerCount") or len(players)),
        players=players,
    )


async def fetch_round_state(config: Config, http: httpx.AsyncClient) -> RoundState | None:
    if not config.round_state_url or not config.round_state_token:
        return None
    try:
        response = await http.get(
            config.round_state_url,
            headers={"Authorization": f"SS14Token {config.round_state_token}"},
            timeout=config.round_state_timeout_s,
        )
        if response.status_code != 200:
            log.warning("round_state: HTTP %s", response.status_code)
            return None
        return parse_round_state(response.json())
    except (httpx.HTTPError, ValueError, KeyError, TypeError) as e:
        log.warning("round_state: %s: %s", type(e).__name__, e)
        return None
