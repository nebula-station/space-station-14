from __future__ import annotations

from datetime import datetime, timedelta, timezone
from uuid import UUID

import pytest

from admin_assist import context
from admin_assist.config import load_config
from admin_assist.context import (
    build_context,
    clean_log_message,
    mentioned_players,
    rank_involved,
    record_scope,
    select_logs,
)
from admin_assist.db import GameDb
from admin_assist.history import ban_text, duration_ru, history_lines
from admin_assist.logtypes import LogImpact, LogType
from admin_assist.render import render_user_message
from admin_assist.roster import RosterPlayer, RoundState

from .conftest import RO_DSN, TEST_CVARS
from .fakes import nonced

A = UUID("00000000-0000-0000-0000-0000000000a1")
M = UUID("00000000-0000-0000-0000-0000000000b2")
K = UUID("00000000-0000-0000-0000-0000000000c3")
BY = UUID("00000000-0000-0000-0000-0000000000d4")
ADM = UUID("00000000-0000-0000-0000-0000000000e5")
NB = UUID("00000000-0000-0000-0000-0000000000f6")
EARLY = UUID("00000000-0000-0000-0000-000000000a07")

LOBBY = datetime(2026, 11, 1, 17, 50, tzinfo=timezone.utc)
T0 = datetime(2026, 11, 1, 18, 0, tzinfo=timezone.utc)


def at(clock: str) -> datetime:
    h, m, s = (int(x) for x in clock.split(":"))
    return T0 + timedelta(hours=h, minutes=m, seconds=s)


def ent(uid: int, name: str, user: UUID | None = None, proto: str = "MobHuman") -> dict:
    data = {"id": uid, "name": name, "prototype": proto}
    if user:
        data["player"] = str(user)
    return data


GRISHA = "Гриша Ковалёв (10/n10, MobHuman, pelmen_enjoyer)"
ALINA = "Алина Штерн (20/n20, MobHuman, Morozko)"


def test_clean_log_message_shortens_entities():
    json = {"actor": ent(10, "Гриша Ковалёв", A), "subject": ent(20, "Алина Штерн", M), "tool": ent(31, "монтировка", None, "Crowbar")}
    message = f"{GRISHA} melee attacked (heavy) {ALINA} using монтировка (31/n31, Crowbar) and dealt 12 damage"
    assert clean_log_message(message, json) == (
        "Гриша Ковалёв (pelmen_enjoyer) melee attacked (heavy) Алина Штерн (Morozko) using монтировка and dealt 12 damage"
    )


def test_clean_log_message_without_json_guesses_from_the_shape():
    assert clean_log_message("A (1/n1, MobHuman, user_a) hit B (2/n2, Crowbar)D and C (3/n3)", None) == "A (user_a) hit B and C"
    assert clean_log_message("Ghost (5/n5, vasya) spoke", {"e": {"id": 5, "player": str(A)}}) == "Ghost (vasya) spoke"
    assert clean_log_message("no entities (light) here (1/2)", {}) == "no entities (light) here (1/2)"


def test_clean_log_message_drops_coordinates_and_erp_text():
    message = f"{ALINA} caused {GRISHA} state to change from Critical to Dead at EntId=65 (0.5, 0.5)"
    assert clean_log_message(message, None).endswith("(pelmen_enjoyer) state to change from Critical to Dead")
    erp = f"Private ERP emote from {ALINA}: нечто: личное"
    assert clean_log_message(erp, None) == "Private ERP emote from Алина Штерн (Morozko): (текст скрыт)"
    assert clean_log_message(erp, None, erp_text=True).endswith(": нечто: личное")
    assert clean_log_message(f"Emote from {ALINA}: машет рукой", None).endswith(": машет рукой")


def row(log_id, type_, impact, message, second=0, json=None):
    return {"admin_log_id": log_id, "type": int(type_), "impact": int(impact), "message": message,
            "json": json, "date": T0 + timedelta(seconds=second or log_id)}


def test_rank_involved_weighs_impact_and_skips_staff_entries():
    rows = [
        row(1, LogType.MeleeHit, LogImpact.Medium, "a hit m"),
        row(2, LogType.Damaged, LogImpact.High, "m crit a"),
        row(3, LogType.Stripping, LogImpact.Low, "by strips a"),
        row(4, LogType.AdminFollow, LogImpact.Low, "adm follows a"),
        row(5, LogType.MeleeHit, LogImpact.Extreme, "m hit by, not the author"),
        row(6, LogType.Chat, LogImpact.Low, "Admin chat from adm: about a"),
    ]
    players = {1: [A, M], 2: [M, A], 3: [BY, A], 4: [ADM, A], 5: [M, BY], 6: [ADM, A]}
    assert rank_involved(A, rows, players) == [M, BY]


def roster(*players: RosterPlayer, round_id: int = 7) -> RoundState:
    return RoundState("nebula", round_id, 3, "InRound", 4265.0, "traitor", "Box", 31, players)


def test_mentioned_players():
    r = roster(
        RosterPlayer(A, "pelmen_enjoyer", "Гриша Ковалёв", "ботаник", (), "dead", False),
        RosterPlayer(M, "Morozko", "Алина Штерн", "бармен", (), "alive", False),
        RosterPlayer(BY, "Tenebris_", "Виктор Грей", "учёный", ("Предатель",), "alive", False),
        RosterPlayer(NB, "bo", "Ян Ли", "повар", (), "alive", False),
    )
    assert mentioned_players(["меня убила бармен", "ник Алину Штерн вроде"], r, A) == [M]
    assert mentioned_players(["это tenebris_ сделал"], r, A) == [BY]
    assert mentioned_players(["Гриша ковалёв тут ни при чём"], r, A) == []
    assert mentioned_players(["бо́льшая часть бара в крови, я был в шоке"], r, A) == []


def test_select_logs_collapses_repeats_and_keeps_the_important_under_the_cap():
    rows = [
        row(1, LogType.Pickup, LogImpact.Low, "a picked up x"),
        row(2, LogType.Chat, LogImpact.Low, "Say from a: привет"),
        row(3, LogType.MeleeHit, LogImpact.Medium, "a hit m for 5"),
        row(4, LogType.MeleeHit, LogImpact.Medium, "a hit m for 5"),
        row(5, LogType.MeleeHit, LogImpact.Medium, "a hit m for 5"),
        row(6, LogType.Drop, LogImpact.Low, "m dropped y"),
        row(7, LogType.Construction, LogImpact.Medium, "m built a wall"),
        row(8, LogType.Damaged, LogImpact.Low, "a state changed from Alive to Critical at (1, 2)"),
        row(9, LogType.AdminFollow, LogImpact.Low, "adm started following a"),
        row(10, LogType.Chat, LogImpact.Low, "Say from m: вот так"),
    ]
    players = {1: [A], 2: [A], 3: [A, M], 4: [A, M], 5: [A, M], 6: [M], 7: [M], 8: [A], 9: [ADM, A], 10: [M]}

    everything = select_logs(rows, players, [A, M], A, cap=150)
    assert [g.ids for g in everything] == [(1,), (2,), (3, 4, 5), (6,), (7,), (8,), (10,)]

    def kept(cap):
        return [g.ids[0] for g in select_logs(rows, players, [A, M], A, cap=cap)]

    assert kept(2) == [3, 8]
    assert kept(4) == [2, 3, 8, 10]
    assert kept(5) == [2, 3, 7, 8, 10]
    assert kept(6) == [2, 3, 6, 7, 8, 10]


def test_duration_and_ban_text():
    assert duration_ru(None) == "навсегда"
    assert duration_ru(timedelta(days=7)) == "7 дней"
    assert duration_ru(timedelta(days=1)) == "1 день"
    assert duration_ru(timedelta(days=3)) == "3 дня"
    assert duration_ru(timedelta(days=21)) == "21 день"
    assert duration_ru(timedelta(hours=11)) == "11 часов"
    assert duration_ru(timedelta(hours=2)) == "2 часа"
    assert duration_ru(timedelta(minutes=30)) == "30 минут"
    ban = {"type": 1, "ban_time": T0, "expiration_time": T0 + timedelta(days=14), "reason": "бросил  смену\nкапитаном",
           "unbanned": True, "roles": ["Captain", "HeadOfSecurity"]}
    assert ban_text(ban) == "2026-11-01 джоббан (Captain, HeadOfSecurity) 14 дней за бросил смену капитаном, снят"


def test_history_lines_address_only_match_needs_a_ban():
    alts = [
        {"user_id": A, "other_user_id": K, "other_user_name": "kvas_lover", "kind": "ip"},
        {"user_id": A, "other_user_id": NB, "other_user_name": "sosed", "kind": "ip"},
        {"user_id": M, "other_user_id": NB, "other_user_name": "sosed", "kind": "hwid"},
    ]
    bans = [{"user_id": K, "type": 0, "ban_time": T0, "expiration_time": None, "reason": "RDM", "unbanned": False, "roles": []}]
    lines = history_lines(
        [(A, "pelmen_enjoyer"), (M, "Morozko")], notes=[], bans=bans, alts=alts,
        playtime_hours={A: 0.4, M: 500.0}, new_player_hours=10,
    )
    assert lines == [
        "pelmen_enjoyer: новый игрок, наиграно меньше часа",
        "pelmen_enjoyer: тот же IP-адрес, что у kvas_lover (2026-11-01 бан навсегда за RDM)",
        "Morozko: тот же HWID, что у sosed (банов нет)",
    ]


async def add_log(c, log_id, clock, type_, impact, message, players, json=None, round_id=7):
    import json as jsonlib

    await c.execute(
        "INSERT INTO admin_log (round_id, admin_log_id, type, impact, date, message, json) VALUES ($1, $2, $3, $4, $5, $6, $7)",
        round_id, log_id, int(type_), int(impact), at(clock), message, jsonlib.dumps(json or {}),
    )
    for user in players:
        await c.execute("INSERT INTO admin_log_player (round_id, log_id, player_user_id) VALUES ($1, $2, $3)", round_id, log_id, user)


@pytest.fixture
async def bar_fight(admin_conn):
    c = admin_conn
    await c.execute("INSERT INTO server (server_id, name) VALUES (1, 'nebula'), (2, 'other')")
    await c.execute("INSERT INTO round (round_id, number, server_id, start_date) VALUES (7, 3, 1, $1), (8, 4, 1, $1)", LOBBY)
    hw_a, hw_m = b"\x01" * 32, b"\x02" * 32
    for user, name, address, hwid in (
        (A, "pelmen_enjoyer", "203.0.113.5", hw_a),
        (M, "Morozko", "198.51.100.7", hw_m),
        (K, "kvas_lover", "192.0.2.99", hw_a),
        (BY, "prohozhiy", "192.0.2.1", None),
        (ADM, "BigBoss", "192.0.2.2", None),
        (NB, "sosed", "198.51.100.7", None),
        (EARLY, "ranniy", "192.0.2.3", None),
    ):
        await c.execute(
            "INSERT INTO player (user_id, first_seen_time, last_seen_time, last_seen_user_name, last_seen_address,"
            " last_seen_hwid, last_seen_hwid_type) VALUES ($1, $2, $2, $3, $4, $5, 1)",
            user, T0, name, address, hwid,
        )
    for user, hours in ((A, 3), (M, 412)):
        await c.execute("INSERT INTO play_time (player_id, tracker, time_spent) VALUES ($1, 'Overall', $2), ($1, 'JobBotanist', $2)",
                        user, timedelta(hours=hours))

    join = "Player {u} joined as {n} on station Nebula with {e} as a {j}."
    await add_log(c, 1, "00:00:00", LogType.RoundStartJoin, LogImpact.Medium,
                  join.format(u="pelmen_enjoyer", n="Гриша Ковалёв", e=GRISHA, j="ботаник"), [A],
                  {"characterName": "Гриша Ковалёв", "jobName": "ботаник", "entity": ent(10, "Гриша Ковалёв", A)})
    await add_log(c, 2, "00:01:00", LogType.LateJoin, LogImpact.Medium,
                  join.format(u="Morozko", n="Алина Штерн", e=ALINA, j="бармен"), [M],
                  {"characterName": "Алина Штерн", "jobName": "бармен", "entity": ent(20, "Алина Штерн", M)})
    await add_log(c, 3, "00:40:00", LogType.MeleeHit, LogImpact.Medium, "old fight", [A, EARLY])

    hit = f"{GRISHA} melee attacked (light) {ALINA} using their hands and dealt 5 damage"
    hit_json = {"actor": ent(10, "Гриша Ковалёв", A), "subject": ent(20, "Алина Штерн", M)}
    logs = [
        (10, "01:07:52", LogType.Chat, LogImpact.Low, f"Say from {GRISHA}: налей бесплатно, я ботаник.", [A]),
        (11, "01:08:10", LogType.Chat, LogImpact.Low, "Say from Прохожий (30/n30, MobHuman, prohozhiy): я просто мимо шёл.", [BY]),
        (12, "01:09:03", LogType.Chat, LogImpact.Low, f"Say from {ALINA}: денег нет — вали из бара.", [M]),
        (13, "01:09:21", LogType.MeleeHit, LogImpact.Medium, hit, [A, M]),
        (14, "01:09:23", LogType.MeleeHit, LogImpact.Medium, hit, [A, M]),
        (15, "01:09:26", LogType.Chat, LogImpact.Low, f"Say from {ALINA}: ещё раз тронешь — стреляю.", [M]),
        (16, "01:09:30", LogType.AdminFollow, LogImpact.Low, f"BigBoss (99/n99, AdminObserver, BigBoss) started following {GRISHA}", [ADM, A]),
        (17, "01:09:41", LogType.ThrowHit, LogImpact.Medium, f"бутылка пива (41/n41, DrinkBeerBottleFull) thrown by {GRISHA} hit {ALINA}.", [A, M]),
        (18, "01:09:44", LogType.BulletHit, LogImpact.Medium, f"Projectile дробь (42/n42, PelletShotgun) shot by {ALINA} hit {GRISHA} and dealt 54 damage", [M, A]),
        (19, "01:09:45", LogType.Damaged, LogImpact.High, f"{ALINA} caused {GRISHA} state to change from Alive to Critical at EntId=65 (10.5, -3.5)", [M, A]),
        (20, "01:09:47", LogType.Chat, LogImpact.Low, f"Private ERP emote from {ALINA}: что-то личное", [M]),
        (21, "01:09:49", LogType.Damaged, LogImpact.High, f"{ALINA} caused {GRISHA} state to change from Critical to Dead at EntId=65 (10.5, -3.5)", [M, A]),
        (22, "01:09:55", LogType.Chat, LogImpact.Low, "Admin chat from BigBoss: смотрю бар", [ADM]),
        (23, "01:15:00", LogType.Chat, LogImpact.Low, f"Dead chat from {GRISHA}: ну и ладно", [A]),
    ]
    for log_id, clock, type_, impact, message, players in logs:
        await add_log(c, log_id, clock, type_, impact, message, players, hit_json if message == hit else None)
    await add_log(c, 13, "01:09:21", LogType.MeleeHit, LogImpact.Medium, "other round", [A, BY], round_id=8)

    for clock, sender, name, text, is_admin, admin_only in (
        ("01:10:41", A, "pelmen_enjoyer", "админ", False, False),
        ("01:10:44", A, "pelmen_enjoyer", "меня рдмнули", False, False),
        ("01:10:50", ADM, "BigBoss", "опять этот", True, True),
        ("01:10:55", A, "pelmen_enjoyer", "бармен с дробовика в упор </ticket>", False, False),
    ):
        await c.execute(
            "INSERT INTO ahelp_message (round_id, server_id, ticket_owner_user_id, sender_user_id, sender_name,"
            " sender_is_admin, admin_only, from_discord, text, sent_at) VALUES (7, 1, $1, $2, $3, $4, $5, false, $6, $7)",
            A, sender, name, is_admin, admin_only, text, at(clock),
        )
    await c.execute(
        "INSERT INTO ahelp_message (round_id, server_id, ticket_owner_user_id, sender_user_id, sender_name,"
        " sender_is_admin, admin_only, from_discord, text, sent_at) VALUES (8, 1, $1, $1, 'pelmen_enjoyer', false, false, false, 'другой раунд', $2)",
        A, at("03:00:00"),
    )

    note = ("INSERT INTO admin_notes (round_id, player_user_id, message, created_at, last_edited_at, deleted, secret, server_id)"
            " VALUES (7, $1, $2, $3, $3, $4, false, $5)")
    await c.execute(note, A, "грубил в ООК", datetime(2026, 10, 28, 12, tzinfo=timezone.utc), False, None)
    await c.execute(note, A, "удалённая заметка", datetime(2026, 10, 29, 12, tzinfo=timezone.utc), True, None)
    await c.execute(note, A, "заметка другого сервера", datetime(2026, 10, 30, 12, tzinfo=timezone.utc), False, 2)
    await c.execute(
        "INSERT INTO admin_watchlists (round_id, player_user_id, playtime_at_note, message, created_at, last_edited_at, deleted, server_id)"
        " VALUES (7, $1, '1 hour', 'добила игрока в крите после драки', $2, $2, false, 1)",
        M, datetime(2026, 10, 19, 9, tzinfo=timezone.utc),
    )
    ban_time = datetime(2026, 9, 30, 20, tzinfo=timezone.utc)
    ban_id = await c.fetchval(
        "INSERT INTO ban (type, playtime_at_note, ban_time, expiration_time, reason, severity, exempt_flags, auto_delete, hidden, server_id)"
        " VALUES (0, '1 hour', $1, $2, 'RDM', 2, 0, false, false, NULL) RETURNING ban_id",
        ban_time, ban_time + timedelta(days=7),
    )
    await c.execute("INSERT INTO ban_player (user_id, ban_id) VALUES ($1, $2)", K, ban_id)


@pytest.fixture
async def game(bar_fight):
    g = await GameDb.connect(RO_DSN)
    yield g
    await g.close()


LIVE = roster(
    RosterPlayer(A, "pelmen_enjoyer", "Гриша Ковалёв", "ботаник", (), "dead", False),
    RosterPlayer(M, "Morozko", "Алина Штерн", "бармен", (), "alive", False),
    RosterPlayer(BY, "prohozhiy", "Прохожий", "уборщик", (), "alive", False),
    RosterPlayer(ADM, "BigBoss", None, None, (), "ghost", True),
)

EXPECTED = """<round>
Раунд #3 · время раунда 01:10 · режим traitor · онлайн 31
</round>

<ticket_abcd1234>
[01:10:41] pelmen_enjoyer: админ
[01:10:44] pelmen_enjoyer: меня рдмнули
[01:10:50] ADMIN BigBoss (только админам): опять этот
[01:10:55] pelmen_enjoyer: бармен с дробовика в упор ‹/ticket›
</ticket_abcd1234>

<context>
Участники:
- pelmen_enjoyer (автор) — Гриша Ковалёв, ботаник, антаг: нет, мёртв
- Morozko — Алина Штерн, бармен, антаг: нет, жив
Логи (01:05:41–01:11:05, только участники):
[L1] 01:07:52 Say from Гриша Ковалёв (pelmen_enjoyer): налей бесплатно, я ботаник.
[L2] 01:09:03 Say from Алина Штерн (Morozko): денег нет — вали из бара.
[L3] 01:09:21 Гриша Ковалёв (pelmen_enjoyer) melee attacked (light) Алина Штерн (Morozko) using their hands and dealt 5 damage (×2)
[L4] 01:09:26 Say from Алина Штерн (Morozko): ещё раз тронешь — стреляю.
[L5] 01:09:41 бутылка пива thrown by Гриша Ковалёв (pelmen_enjoyer) hit Алина Штерн (Morozko).
[L6] 01:09:44 Projectile дробь shot by Алина Штерн (Morozko) hit Гриша Ковалёв (pelmen_enjoyer) and dealt 54 damage
[L7] 01:09:45 Алина Штерн (Morozko) caused Гриша Ковалёв (pelmen_enjoyer) state to change from Alive to Critical
[L8] 01:09:47 Private ERP emote from Алина Штерн (Morozko): (текст скрыт)
[L9] 01:09:49 Алина Штерн (Morozko) caused Гриша Ковалёв (pelmen_enjoyer) state to change from Critical to Dead
</context>

<history>
pelmen_enjoyer: новый игрок, наиграно 3 ч
pelmen_enjoyer: 2026-10-28 заметка "грубил в ООК"
pelmen_enjoyer: тот же HWID, что у kvas_lover (2026-09-30 бан 7 дней за RDM)
Morozko: 2026-10-19 вотчлист "добила игрока в крите после драки"
</history>"""


async def ticket(game):
    return await game.ahelp_messages(A, 7)


@pytest.mark.db
async def test_build_context_end_to_end(game, config):
    messages = await ticket(game)
    assert [m["text"] for m in messages][:2] == ["админ", "меня рдмнули"] and len(messages) == 4

    ctx = await build_context(game, config, owner=A, round_id=7, messages=messages, roster=LIVE, scope_server_id=1)
    assert render_user_message(ctx, "abcd1234") == nonced(EXPECTED, "abcd1234")

    assert [log.log_ids for log in ctx.logs] == [(10,), (12,), (13, 14), (15,), (17,), (18,), (19,), (20,), (21,)]
    assert ctx.round_id == 7
    assert [p.user_id for p in ctx.participants] == [A, M]


@pytest.mark.db
async def test_no_raw_identifiers_reach_the_prompt(game, config):
    ctx = await build_context(game, config, owner=A, round_id=7, messages=await ticket(game), roster=LIVE)
    text = render_user_message(ctx)
    for secret in ("203.0.113", "198.51.100", "192.0.2", "0101", "AQEB", str(A), str(M), "10/n10"):
        assert secret not in text


@pytest.mark.db
async def test_record_scope_rule(game, config):
    messages = await ticket(game)
    everything = await build_context(game, config, owner=A, round_id=7, messages=messages, roster=LIVE, scope_server_id=None)
    assert any("заметка другого сервера" in h for h in everything.history)
    other = await build_context(game, config, owner=A, round_id=7, messages=messages, roster=LIVE, scope_server_id=2)
    assert any("заметка другого сервера" in h and "грубил в ООК" in h for h in other.history)
    assert not any("вотчлист" in h for h in other.history)
    assert any("kvas_lover (2026-09-30 бан" in h for h in other.history)

    assert await record_scope(game, config) is None
    isolated = load_config({**TEST_CVARS, "nebula.isolate_records": "true", "adminlogs.server_name": "other"})
    assert await record_scope(game, isolated) == 2
    with pytest.raises(LookupError):
        await record_scope(game, load_config({**TEST_CVARS, "nebula.isolate_records": "true", "adminlogs.server_name": "typo"}))


@pytest.mark.db
async def test_without_the_roster_the_logs_fill_in(game, config):
    for unusable in (None, roster(round_id=8)):
        ctx = await build_context(game, config, owner=A, round_id=7, messages=await ticket(game), roster=unusable)
        text = render_user_message(ctx)
        assert "Раунд #3 · время раунда 01:10 · режим неизвестен · онлайн неизвестно" in text
        assert "- pelmen_enjoyer (автор) — Гриша Ковалёв, ботаник, антаг: неизвестно, не в сети" in text
        assert "- Morozko — Алина Штерн, бармен, антаг: неизвестно, не в сети" in text
        assert len(ctx.logs) == 9


@pytest.mark.db
async def test_a_named_player_is_pulled_in_even_without_contact(game, config, admin_conn):
    await admin_conn.execute(
        "INSERT INTO ahelp_message (round_id, server_id, ticket_owner_user_id, sender_user_id, sender_name,"
        " sender_is_admin, admin_only, from_discord, text, sent_at) VALUES (7, 1, $1, $1, 'pelmen_enjoyer', false, false, false, $2, $3)",
        A, "и prohozhiy всё видел", at("01:10:58"),
    )
    ctx = await build_context(game, config, owner=A, round_id=7, messages=await ticket(game), roster=LIVE)
    assert [p.username for p in ctx.participants] == ["pelmen_enjoyer", "prohozhiy", "Morozko"]
    assert any("я просто мимо шёл" in log.text for log in ctx.logs)


@pytest.mark.db
async def test_caps(game):
    config = load_config({**TEST_CVARS, "admin_assist.context_max_log_lines": "3", "admin_assist.context_max_participants": "1"})
    ctx = await build_context(game, config, owner=A, round_id=7, messages=await ticket(game), roster=LIVE)
    assert [p.username for p in ctx.participants] == ["pelmen_enjoyer"]
    assert len(ctx.logs) == 3
    assert [log.log_ids[0] for log in ctx.logs] == [10, 19, 21]


@pytest.mark.db
async def test_ticket_outside_any_round(game, config, admin_conn):
    await admin_conn.execute(
        "INSERT INTO ahelp_message (round_id, server_id, ticket_owner_user_id, sender_user_id, sender_name,"
        " sender_is_admin, admin_only, from_discord, text, sent_at) VALUES (NULL, 1, $1, $1, 'Morozko', false, false, false, 'ау', $2)",
        M, at("05:00:01"),
    )
    messages = await game.ahelp_messages(M, None)
    ctx = await build_context(game, config, owner=M, round_id=None, messages=messages, roster=LIVE)
    text = render_user_message(ctx, "n")
    assert "Раунд не идёт" in text
    assert "[23:00:01] Morozko: ау" in text
    assert "- Morozko (автор) — персонаж неизвестен, должность неизвестна, антаг: неизвестно, не в сети\nЛоги: записей нет" in text
    assert 'Morozko: 2026-10-19 вотчлист' in text


@pytest.mark.db
async def test_ahelp_messages_window_and_limit(game):
    assert len(await game.ahelp_messages(A, 7, since=at("01:10:44"), until=at("01:10:50"))) == 2
    newest = await game.ahelp_messages(A, 7, limit=2)
    assert [m["text"] for m in newest] == ["опять этот", "бармен с дробовика в упор </ticket>"]
    assert [m["text"] for m in await game.ahelp_messages(A, 8)] == ["другой раунд"]


@pytest.mark.db
async def test_round_started_at_is_the_first_join_not_the_lobby(game):
    assert await game.round_started_at(7) == T0
    assert await game.round_started_at(8) is None
    assert (await game.round(7))["start_date"] == LOBBY


def test_builder_reads_admin_log_only_through_the_capped_helper():
    import inspect

    assert "FROM admin_log" not in inspect.getsource(context)


def test_mentioned_players_by_job():
    officers = [RosterPlayer(UUID(int=100 + n), f"sec_{n}", f"Боец Номер{n}", "Офицер службы безопасности", (), "alive", False) for n in range(3)]
    r = roster(
        RosterPlayer(A, "pelmen_enjoyer", "Гриша Ковалёв", "ботаник", (), "dead", False),
        RosterPlayer(M, "Morozko", "Алина Штерн", "бармен", (), "alive", False),
        RosterPlayer(BY, "Tenebris_", "Виктор Грей", "Глава службы безопасности", (), "alive", False),
        RosterPlayer(NB, "bo", "Ян Ли", "Капитан", (), "alive", False),
        *officers,
    )
    assert mentioned_players(["меня убил бармен ни за что"], r, A) == [M]
    assert mentioned_players(["капитана спросите, он видел"], r, A) == [NB]
    assert mentioned_players(["кэп и гсб стояли рядом"], r, A) == [NB, BY]
    assert mentioned_players(["офицер меня избил"], r, A) == []
    assert mentioned_players(["служба безопасности беспредельничает"], r, A) == []
    assert mentioned_players(["я ботаник, выращивал траву"], r, A) == []
    assert mentioned_players(["бармен и Виктор Грей"], r, A) == [BY, M]
