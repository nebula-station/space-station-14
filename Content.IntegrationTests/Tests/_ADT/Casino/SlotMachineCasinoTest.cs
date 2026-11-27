// SPDX-License-Identifier: AGPL-3.0-only
// Ported from https://github.com/AdventureTimeSS14/space_station_ADT/blob/921015157ff5a0e4d3156c1fccc0a1f3c34c0a96/Content.IntegrationTests/Tests/ADT/Casino/SlotMachineCasinoTest.cs

using Content.IntegrationTests.Tests.Interaction;
using Content.Server._ADT.Casino;
using Content.Shared._ADT.Casino;
using Content.Shared.Power.EntitySystems;

namespace Content.IntegrationTests.Tests._ADT.Casino;

public sealed class SlotMachineCasinoTest : InteractionTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, DummyTicker = false };

    [Test]
    public async Task TestCasinoBuiOpensAndSpins()
    {
        var machine = await Spawn("ADTMachineSlotMachineCasino", TargetCoords);
        await RunUntilSynced();

        await Server.WaitPost(() =>
            SEntMan.System<SharedPowerReceiverSystem>().SetNeedsPower(SEntMan.GetEntity(machine), false));
        await RunUntilSynced();

        Assert.That(IsUiOpen(SlotMachineCasinoUiKey.Key), Is.False, "Casino BUI opened before activation.");

        await Activate(machine);
        await RunUntilSynced();

        Assert.That(IsUiOpen(SlotMachineCasinoUiKey.Key), Is.True, "Casino BUI did not open on activation.");

        var sSystem = SEntMan.System<SlotMachineCasinoSystem>();
        Assert.That(sSystem.GlobalPlays, Is.GreaterThanOrEqualTo(1), "UI open did not register in global stats.");

        var sMachine = SEntMan.GetComponent<SlotMachineCasinoComponent>(ToServer(machine));

        await Server.WaitPost(() => sMachine.Balance = 50);
        await Server.WaitPost(() => sSystem.TrySpin((ToServer(machine), sMachine), null));

        for (var i = 0; i < 40 && sMachine.SpinHistory.Count == 0; i++)
            await RunTicks(30);

        Assert.That(sMachine.SpinHistory.Count, Is.GreaterThanOrEqualTo(1), "Spin did not produce a result.");
        Assert.That(sMachine.Reels[0].Length, Is.EqualTo(3), "Reels were not populated.");

        await Server.WaitPost(() =>
        {
            sMachine.FreeSpinsRemaining = 0;
            sMachine.AutoSpin = false;
            sMachine.AutoSpinRemaining = 0;
            sMachine.NextAutoSpinTime = TimeSpan.Zero;
        });
        await RunUntilSynced();
    }
}
