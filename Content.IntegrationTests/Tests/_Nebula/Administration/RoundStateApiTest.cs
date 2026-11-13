// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Content.IntegrationTests.Fixtures;
using Content.Server.Administration;
using Content.Server.GameTicking;

namespace Content.IntegrationTests.Tests._Nebula.Administration;

[TestFixture]
public sealed class RoundStateApiTest : GameTest
{
    private static readonly string[] RoundFields =
    [
        "ServerName", "RoundId", "RoundNumber", "RunLevel", "RoundDurationSeconds", "GamePreset", "MapId", "MapName",
        "PlayerCount", "Players",
    ];

    private static readonly string[] PlayerFields =
    [
        "UserId", "Username", "CharacterName", "JobId", "JobName", "AntagRoles", "State", "IsAdmin",
    ];

    private static readonly string[] States = ["lobby", "alive", "crit", "dead", "ghost"];

    public override PoolSettings PoolSettings => new()
    {
        DummyTicker = false,
        Connected = true,
        Dirty = true,
    };

    [Test]
    public async Task ConnectedPlayerIsListedWithNothingExtra()
    {
        var api = Server.ResolveDependency<ServerApi>();
        var ticker = Server.System<ServerGameTicker>();
        var build = typeof(ServerApi).GetMethod("BuildRoundState", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var json = "";
        await Server.WaitPost(() => json = JsonSerializer.Serialize(build.Invoke(api, null)));
        TestContext.Out.WriteLine(json);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var players = root.GetProperty("Players").EnumerateArray().ToList();
        var me = players.Single(player => player.GetProperty("UserId").GetGuid() == ServerSession!.UserId.UserId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(root.EnumerateObject().Select(field => field.Name), Is.EquivalentTo(RoundFields));
            Assert.That(me.EnumerateObject().Select(field => field.Name), Is.EquivalentTo(PlayerFields));
            Assert.That(root.GetProperty("RoundId").GetInt32(), Is.EqualTo(ticker.RoundDatabaseId));
            Assert.That(root.GetProperty("RoundNumber").GetInt32(), Is.EqualTo(ticker.RoundId));
            Assert.That(root.GetProperty("PlayerCount").GetInt32(), Is.EqualTo(players.Count));
            Assert.That(me.GetProperty("Username").GetString(), Is.EqualTo(ServerSession!.Name));
            Assert.That(States, Does.Contain(me.GetProperty("State").GetString()));
        }
    }
}
