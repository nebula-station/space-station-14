// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable
using System.Linq;
using System.Net;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Database;
using Content.Shared._Nebula.CCVar;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests._Nebula.Database;

[TestFixture]
public sealed class IsolateRecordsTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        Connected = false,
        Dirty = true,
    };

    [Test]
    public async Task BansAndNotesStayOnTheirServer()
    {
        var db = Server.ResolveDependency<IServerDbManager>();
        var user = new NetUserId(Guid.NewGuid());

        await db.UpdatePlayerRecordAsync(user, "isolate-test", IPAddress.Loopback, null);

        await OverrideCVar(Side.Server, NCCVars.IsolateRecords, false);
        await db.AddBanAsync(MakeBan(user, "global"));
        await AddNote(db, user, "global-note");

        await OverrideCVar(Side.Server, CCVars.AdminLogsServerName, "pegas");
        await OverrideCVar(Side.Server, NCCVars.IsolateRecords, true);
        var pegas = await db.AddBanAsync(MakeBan(user, "pegas"));
        await AddNote(db, user, "pegas-note");
        await db.AddAdminWatchlist(null, user.UserId, TimeSpan.Zero, "pegas-watch", user.UserId, DateTimeOffset.UtcNow, null);

        await OverrideCVar(Side.Server, CCVars.AdminLogsServerName, "perseus");
        var perseus = await db.AddBanAsync(MakeBan(user, "perseus"));
        await AddNote(db, user, "perseus-note");

        var bans = await db.GetBansAsync(null, user, null, null);
        Assert.That(bans.Select(ban => ban.Reason), Is.EquivalentTo(new[] { "global", "perseus" }));
        Assert.That(bans.Single(ban => ban.Reason == "global").ServerId, Is.Null);
        Assert.That(perseus.ServerId, Is.Not.Null);
        Assert.That(pegas.ServerId, Is.Not.Null);
        Assert.That(pegas.ServerId, Is.Not.EqualTo(perseus.ServerId));

        // id lookup still finds the other servers ban, the list just doesnt
        Assert.That(await db.GetBanAsync(pegas.Id!.Value), Is.Not.Null);

        var notes = (await db.GetAllAdminRemarks(user.UserId))
            .OfType<AdminNoteRecord>()
            .Select(note => note.Message);
        Assert.That(notes, Is.EquivalentTo(new[] { "global-note", "perseus-note" }));

        var watches = await db.GetActiveWatchlists(user.UserId);
        Assert.That(watches, Is.Empty);
    }

    private static BanDef MakeBan(NetUserId user, string reason)
    {
        return new BanDef(
            null,
            BanType.Server,
            [user],
            [],
            [],
            DateTimeOffset.UtcNow,
            null,
            [],
            TimeSpan.Zero,
            reason,
            NoteSeverity.Minor,
            null,
            null);
    }

    private static Task AddNote(IServerDbManager db, NetUserId user, string message)
    {
        return db.AddAdminNote(null, user.UserId, TimeSpan.Zero, message, NoteSeverity.Minor, false, user.UserId, DateTimeOffset.UtcNow, null);
    }
}
