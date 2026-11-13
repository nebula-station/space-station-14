// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Administration;
using Content.Server.Administration.Systems;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared._Nebula.CCVar;
using Content.Shared.Administration;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests._Nebula.Administration;

[TestFixture]
public sealed class AHelpPersistTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        Connected = false,
        Dirty = true,
    };

    [Test]
    [EnsureCVar(Side.Server, typeof(NCCVars), nameof(NCCVars.AHelpPersist), true)]
    public async Task MessagesAreStoredAsSent()
    {
        var db = Server.ResolveDependency<IServerDbManager>();
        var bwoink = Server.System<BwoinkSystem>();
        var ticker = Server.System<ServerGameTicker>();
        var owner = new NetUserId(Guid.NewGuid());
        int? round = ticker.RoundDatabaseId == 0 ? null : ticker.RoundDatabaseId;

        await Server.WaitPost(() =>
        {
            Send(bwoink, owner, "первое <b>сообщение</b>", "discord-admin", adminOnly: false);
            Send(bwoink, owner, "second", "discord-admin", adminOnly: true);
        });

        List<AHelpMessage> stored = new();
        await PoolManager.WaitUntil(Server, async () =>
        {
            stored = await db.GetAHelpMessages(owner, round);
            return stored.Count == 2;
        });

        var server = await Server.ResolveDependency<ServerDbEntryManager>().ServerEntity;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(stored.Select(message => message.Text), Is.EqualTo(new[] { "первое <b>сообщение</b>", "second" }));
            Assert.That(stored.Select(message => message.AdminOnly), Is.EqualTo(new[] { false, true }));
            Assert.That(stored.All(message => message.TicketOwnerUserId == owner.UserId));
            Assert.That(stored.All(message => message.SenderUserId == Guid.Empty));
            Assert.That(stored.All(message => message.SenderName == "discord-admin"));
            Assert.That(stored.All(message => message.SenderIsAdmin && message.FromDiscord));
            Assert.That(stored.All(message => message.ServerId == server.Id));
            Assert.That(stored.All(message => message.RoundId == round));
            Assert.That(stored[0].Id, Is.LessThan(stored[1].Id));
            Assert.That(stored[0].SentAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(5)));
        }
    }

    [Test]
    public async Task NothingIsStoredWhenTurnedOff()
    {
        var db = Server.ResolveDependency<IServerDbManager>();
        var bwoink = Server.System<BwoinkSystem>();
        var ticker = Server.System<ServerGameTicker>();
        var owner = new NetUserId(Guid.NewGuid());
        int? round = ticker.RoundDatabaseId == 0 ? null : ticker.RoundDatabaseId;

        await OverrideCVar(Side.Server, NCCVars.AHelpPersist, false);
        await Server.WaitPost(() => Send(bwoink, owner, "not stored", "discord-admin", adminOnly: false));

        await OverrideCVar(Side.Server, NCCVars.AHelpPersist, true);
        await Server.WaitPost(() => Send(bwoink, owner, "marker", "discord-admin", adminOnly: false));

        List<AHelpMessage> stored = new();
        await PoolManager.WaitUntil(Server, async () =>
        {
            stored = await db.GetAHelpMessages(owner, round);
            return stored.Count > 0;
        });

        Assert.That(stored.Select(message => message.Text), Is.EqualTo(new[] { "marker" }));
    }

    private static void Send(BwoinkSystem bwoink, NetUserId owner, string text, string sender, bool adminOnly)
    {
        var message = new SharedBwoinkSystem.BwoinkTextMessage(owner, SharedBwoinkSystem.SystemUserId, text, adminOnly: adminOnly);
        bwoink.OnWebhookBwoinkTextMessage(message, new ServerApi.BwoinkActionBody
        {
            Text = text,
            Username = sender,
            Guid = owner.UserId,
            UserOnly = false,
            WebhookUpdate = false,
            RoleName = "Admin",
            RoleColor = "#ffffff",
        });
    }
}
