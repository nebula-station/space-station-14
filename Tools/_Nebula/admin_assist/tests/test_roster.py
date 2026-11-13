from __future__ import annotations

from uuid import UUID

import httpx

from admin_assist.config import load_config
from admin_assist.roster import fetch_round_state, parse_round_state

from .conftest import TEST_CVARS

U1 = "00000000-0000-0000-0000-0000000000a1"

PAYLOAD = {
    "ServerName": "nebula",
    "RoundId": 7,
    "RoundNumber": 3,
    "RunLevel": "InRound",
    "RoundDurationSeconds": 4265.5,
    "GamePreset": "traitor",
    "MapId": "Box",
    "MapName": "Box Station",
    "PlayerCount": 2,
    "Players": [
        {"UserId": U1, "Username": "pelmen_enjoyer", "CharacterName": "Гриша Ковалёв", "JobId": "Botanist",
         "JobName": "ботаник", "AntagRoles": [{"Id": "Traitor", "Name": "Предатель"}], "State": "dead", "IsAdmin": False},
        {"UserId": "00000000-0000-0000-0000-0000000000e5", "Username": "BigBoss", "CharacterName": None, "JobId": None,
         "JobName": None, "AntagRoles": [], "State": "lobby", "IsAdmin": True},
    ],
}

ENV = {**TEST_CVARS, "admin_assist.round_state_url": "http://127.0.0.1:1212/admin_assist/round_state", "admin_assist.round_state_token": "tok"}


def test_parse_round_state():
    state = parse_round_state(PAYLOAD)
    assert (state.round_id, state.round_number, state.gamemode, state.player_count) == (7, 3, "traitor", 2)
    player = state.by_user()[UUID(U1)]
    assert (player.username, player.character, player.job, player.antag, player.state) == (
        "pelmen_enjoyer", "Гриша Ковалёв", "ботаник", ("Предатель",), "dead",
    )
    lobby = state.players[1]
    assert (lobby.character, lobby.job, lobby.antag, lobby.is_admin) == (None, None, (), True)


async def fetch(handler, env=ENV):
    async with httpx.AsyncClient(transport=httpx.MockTransport(handler)) as http:
        return await fetch_round_state(load_config(env), http)


async def test_fetch_sends_its_own_token():
    seen = []

    def handler(request: httpx.Request) -> httpx.Response:
        seen.append((str(request.url), request.headers["authorization"]))
        return httpx.Response(200, json=PAYLOAD)

    state = await fetch(handler)
    assert state.round_id == 7
    assert seen == [("http://127.0.0.1:1212/admin_assist/round_state", "SS14Token tok")]


async def test_fetch_gives_none_instead_of_failing():
    def boom(request):
        raise httpx.ConnectError("refused")

    assert await fetch(boom) is None
    assert await fetch(lambda r: httpx.Response(404)) is None
    assert await fetch(lambda r: httpx.Response(429)) is None
    assert await fetch(lambda r: httpx.Response(200, text="<html>")) is None
    assert await fetch(lambda r: httpx.Response(200, json={"RoundId": 7})) is None


async def test_fetch_is_off_without_a_url_or_token():
    def never(request):
        raise AssertionError("no request expected")

    assert await fetch(never, {**ENV, "admin_assist.round_state_token": ""}) is None
    assert await fetch(never, {**ENV, "admin_assist.round_state_url": ""}) is None
