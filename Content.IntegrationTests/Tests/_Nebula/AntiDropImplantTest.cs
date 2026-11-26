// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Medical.Shared.Body;
using Content.Shared._Nebula.Implants;
using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Nebula;

/// <summary>
/// The anti-drop implant locks the item held in the hand it is installed into.
/// </summary>
[TestFixture]
[TestOf(typeof(AntiDropImplantSystem))]
public sealed class AntiDropImplantTest
{
    private static readonly ProtoId<OrganCategoryPrototype> HandImplant = "HandImplant";

    [TestCase("left")]
    [TestCase("right")]
    public async Task LocksItemInImplantedHand(string handId)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var server = pair.Server;
        var entMan = server.EntMan;
        var body = entMan.System<BodySystem>();
        var part = entMan.System<BodyPartSystem>();
        var hands = entMan.System<SharedHandsSystem>();
        var antiDrop = entMan.System<AntiDropImplantSystem>();

        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var human = entMan.SpawnAtPosition("MobHuman", map.GridCoords);
            var hand = body.EnumerateOrgans<HandOrganComponent>(human).First(h => h.Comp2.HandID == handId).Owner;

            var implant = entMan.SpawnAtPosition("HandImplantAntiDrop", map.GridCoords);
            part.TryAddSlot(hand, HandImplant);
            Assert.That(part.InsertOrgan(hand, implant), "Implant was not inserted.");
            Assert.That(entMan.GetComponent<ChildOrganComponent>(implant).Parent, Is.EqualTo(hand),
                "Implant is parented to the wrong part.");

            var item = entMan.SpawnAtPosition("Crowbar", map.GridCoords);
            hands.TrySetActiveHand(human, handId);
            Assert.That(hands.GetActiveHand(human), Is.EqualTo(handId));
            Assert.That(hands.TryPickup(human, item, handId));

            var comp = entMan.GetComponent<AntiDropImplantComponent>(implant);
            Assert.That(antiDrop.TryLock((implant, comp), human), "Item in the implanted hand was not locked.");
            Assert.That(comp.Locked, Is.EqualTo(item));
        });

        await pair.CleanReturnAsync();
    }

    [TestCase("left")]
    [TestCase("right")]
    public async Task ClientPredictsLockInImplantedHand(string handId)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, Dirty = true });
        var server = pair.Server;
        var client = pair.Client;
        var entMan = server.EntMan;
        var body = entMan.System<BodySystem>();
        var part = entMan.System<BodyPartSystem>();
        var hands = entMan.System<SharedHandsSystem>();

        var map = await pair.CreateTestMap();
        EntityUid human = default;
        EntityUid hand = default;
        EntityUid implant = default;

        await server.WaitAssertion(() =>
        {
            human = entMan.SpawnAtPosition("MobHuman", map.GridCoords);
            server.PlayerMan.SetAttachedEntity(pair.Player!, human);
            hand = body.EnumerateOrgans<HandOrganComponent>(human).First(h => h.Comp2.HandID == handId).Owner;

            implant = entMan.SpawnAtPosition("HandImplantAntiDrop", map.GridCoords);
            part.TryAddSlot(hand, HandImplant);
            Assert.That(part.InsertOrgan(hand, implant), "Implant was not inserted.");

            var item = entMan.SpawnAtPosition("Crowbar", map.GridCoords);
            hands.TrySetActiveHand(human, handId);
            Assert.That(hands.TryPickup(human, item, handId));
        });

        await pair.RunTicksSync(10);

        var cEntMan = client.EntMan;
        var cHuman = pair.ToClientUid(human);
        var cHand = pair.ToClientUid(hand);
        var cImplant = pair.ToClientUid(implant);

        await client.WaitAssertion(() =>
        {
            Assert.That(cEntMan.GetComponent<ChildOrganComponent>(cImplant).Parent, Is.EqualTo(cHand),
                "Client has the implant parented to the wrong part.");
            Assert.That(cEntMan.System<SharedHandsSystem>().GetActiveHand(cHuman), Is.EqualTo(handId));

            var comp = cEntMan.GetComponent<AntiDropImplantComponent>(cImplant);
            Assert.That(cEntMan.System<AntiDropImplantSystem>().TryLock((cImplant, comp), cHuman),
                "Client prediction refused to lock the item.");
        });

        await pair.CleanReturnAsync();
    }
}
