// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Database;

namespace Content.IntegrationTests.Tests._Nebula.Database;

[TestFixture]
public sealed class RoundNumberTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        Connected = false,
        Dirty = true,
    };

    [Test]
    public async Task EachServerCountsItsOwnRounds()
    {
        var db = Server.ResolveDependency<IServerDbManager>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var perseus = await db.AddOrGetServer($"perseus-{suffix}");
        var pegas = await db.AddOrGetServer($"pegas-{suffix}");

        var first = await db.AddNewRound(perseus);
        var second = await db.AddNewRound(perseus);
        var third = await db.AddNewRound(perseus);
        var pegasFirst = await db.AddNewRound(pegas);

        Assert.That(new[] { first.Number, second.Number, third.Number }, Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(pegasFirst.Number, Is.EqualTo(1));
        Assert.That(pegasFirst.Id, Is.GreaterThan(third.Id));

        Assert.That(await db.FindRoundId(perseus.Id, 1), Is.EqualTo(first.Id));
        Assert.That(await db.FindRoundId(pegas.Id, 1), Is.EqualTo(pegasFirst.Id));
        Assert.That(await db.FindRoundId(pegas.Id, 2), Is.Null);
    }
}
