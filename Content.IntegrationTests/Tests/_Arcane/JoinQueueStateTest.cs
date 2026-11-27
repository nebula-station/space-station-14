// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/dfbda555db9acabf3827cf3b0785c6deaa5c1a8b/Content.IntegrationTests/Tests/_Arcane/JoinQueueStateTest.cs

using Content.Server._Arcane.JoinQueue;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class JoinQueueStateTest
{

    [Test]
    public void ASlowerProfileLoadCannotBeOvertakenByALaterPlayer()
    {
        var queue = new JoinQueueState<object>();
        var first = new object();
        var second = new object();
        var firstId = new NetUserId(Guid.NewGuid());
        var secondId = new NetUserId(Guid.NewGuid());
        queue.Connect(firstId, first, TimeSpan.Zero);
        queue.Connect(secondId, second, TimeSpan.Zero);
        queue.MarkReady(secondId, second);
        Assert.That(queue.TryReserve(out _), Is.False);
        queue.MarkReady(firstId, first);
        Assert.That(queue.TryReserve(out var entry), Is.True);
        Assert.That(entry.Session, Is.SameAs(first));
        Assert.That(queue.TryReserve(out entry), Is.True);
        Assert.That(entry.Session, Is.SameAs(second));
    }

    [Test]
    public void OfflineReservationsDoNotBlockSlotsAndExpiredReservationsRejoinAtTheEnd()
    {
        var queue = new JoinQueueState<object>();
        var disconnected = new object();
        var second = new object();
        var third = new object();
        var returning = new object();
        var user = new NetUserId(Guid.NewGuid());
        var secondId = new NetUserId(Guid.NewGuid());
        var thirdId = new NetUserId(Guid.NewGuid());
        queue.Connect(user, disconnected, TimeSpan.Zero);
        queue.Disconnect(user, disconnected, TimeSpan.FromMinutes(2));
        queue.Connect(secondId, second, TimeSpan.FromSeconds(10));
        queue.MarkReady(secondId, second);
        Assert.That(queue.TryReserve(out var entry), Is.True);
        Assert.That(entry.Session, Is.SameAs(second));
        queue.Connect(thirdId, third, TimeSpan.FromSeconds(20));
        queue.MarkReady(thirdId, third);
        queue.Connect(user, returning, TimeSpan.FromMinutes(2));
        queue.MarkReady(user, returning);
        Assert.That(queue.TryReserve(out entry), Is.True);
        Assert.That(entry.Session, Is.SameAs(third));
        Assert.That(queue.TryReserve(out entry), Is.True);
        Assert.That(entry.Session, Is.SameAs(returning));
    }
}
