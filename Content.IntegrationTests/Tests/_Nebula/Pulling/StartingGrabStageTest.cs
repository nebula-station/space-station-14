// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Movement.Pulling;
using Content.Shared.CombatMode;
using Content.Shared.Inventory;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Trauma.Common.MartialArts;

namespace Content.IntegrationTests.Tests._Nebula.Pulling;

[TestFixture]
[TestOf(typeof(StartingGrabStageSystem))]
public sealed class StartingGrabStageTest
{
    private const string Mob = "MobHuman";
    private const string Gloves = "ClothingHandsGlovesGuerrilla";

    [TestCase(true, true, GrabStage.Hard)]
    [TestCase(false, true, GrabStage.Soft)]
    [TestCase(true, false, GrabStage.No)]
    public async Task PullStartsAtExpectedStage(bool wearGloves, bool combatMode, GrabStage expected)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entMan = server.EntMan;

        await server.WaitAssertion(() =>
        {
            var puller = entMan.SpawnEntity(Mob, map.GridCoords);
            var target = entMan.SpawnEntity(Mob, map.GridCoords);

            if (wearGloves)
            {
                var gloves = entMan.SpawnEntity(Gloves, map.GridCoords);
                Assert.That(entMan.System<InventorySystem>().TryEquip(puller, gloves, "gloves", force: true));
            }

            entMan.System<SharedCombatModeSystem>().SetInCombatMode(puller, combatMode);

            Assert.That(entMan.System<PullingSystem>().TryStartPull(puller, target));
            Assert.That(entMan.GetComponent<PullerComponent>(puller).GrabStage, Is.EqualTo(expected));
            Assert.That(entMan.GetComponent<PullableComponent>(target).GrabStage, Is.EqualTo(expected));
        });

        await pair.CleanReturnAsync();
    }
}
