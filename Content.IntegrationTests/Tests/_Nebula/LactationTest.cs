// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Nebula.ERP.Lactation;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Traits;

namespace Content.IntegrationTests.Tests._Nebula;

[TestFixture]
[TestOf(typeof(LactationSystem))]
public sealed class LactationTest
{
    private static readonly ProtoId<TraitPrototype> Trait = "Lactation";

    [Test]
    public async Task TraitGivesMilkableSolution()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;
        var solutions = entMan.System<SharedSolutionContainerSystem>();
        var harvest = entMan.System<HarvestableSolutionSystem>();
        var inventory = entMan.System<InventorySystem>();
        var map = await pair.CreateTestMap();
        var mob = EntityUid.Invalid;

        await server.WaitAssertion(() =>
        {
            mob = entMan.SpawnEntity("MobHuman", map.GridCoords);
            server.PlayerMan.SetAttachedEntity(pair.Player!, mob);
            entMan.AddComponents(mob, protoMan.Index(Trait).Components, false);

            Assert.That(solutions.TryGetSolution(mob, "breasts", out _, out var milk), "No breast solution was spawned.");
            Assert.That(milk!.GetTotalPrototypeQuantity("BreastMilk").Float(), Is.GreaterThan(0f));
            var before = milk.Volume;

            var beaker = entMan.SpawnEntity("Beaker", map.GridCoords);

            var jumpsuit = entMan.SpawnEntity("ClothingUniformJumpsuitColorGrey", map.GridCoords);
            Assert.That(inventory.TryEquip(mob, jumpsuit, "jumpsuit", silent: true, force: true));
            Assert.That(harvest.TryHarvest(mob, mob, beaker), Is.False, "Milked through a jumpsuit.");

            Assert.That(inventory.TryUnequip(mob, "jumpsuit", silent: true, force: true));

            var bra = entMan.SpawnEntity("ClothingUndershirtBabydoll", map.GridCoords);
            Assert.That(inventory.TryEquip(mob, bra, "underweart", silent: true, force: true));
            Assert.That(harvest.TryHarvest(mob, mob, beaker), Is.False, "Milked through a bra.");

            Assert.That(inventory.TryUnequip(mob, "underweart", silent: true, force: true));
            Assert.That(harvest.TryHarvest(mob, mob, beaker), "Milking failed.");

            Assert.That(solutions.TryGetSolution(beaker, "beaker", out _, out var beakerSol));
            Assert.That(beakerSol!.GetTotalPrototypeQuantity("BreastMilk"), Is.EqualTo(before));
            Assert.That(milk.Volume.Float(), Is.Zero);

            var lactation = entMan.GetComponent<LactationComponent>(mob);
            var mapInit = new MapInitEvent();
            entMan.EventBus.RaiseComponentEvent(mob, lactation, ref mapInit);
            Assert.That(solutions.EnumerateSolutions(mob).Count(s => s.Name == "breasts"), Is.EqualTo(1));
        });

        await pair.RunTicksSync(10);
        var netMob = entMan.GetNetEntity(mob);
        await pair.Client.WaitAssertion(() =>
        {
            var clientEnt = pair.Client.EntMan;
            var clientMob = clientEnt.GetEntity(netMob);
            Assert.That(clientEnt.TryGetComponent<HarvestableSolutionComponent>(clientMob, out var comp));
            Assert.That(comp!.VerbText.Id, Is.EqualTo("lactation-verb-milk"));
            Assert.That(comp.SolutionName, Is.EqualTo("breasts"));
            Assert.That(clientEnt.HasComponent<LactationComponent>(clientMob));
        });

        await pair.CleanReturnAsync();
    }
}
