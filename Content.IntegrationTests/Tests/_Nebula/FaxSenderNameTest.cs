// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Fax;
using Content.Server.Station.Systems;
using Content.Shared._Nebula.Fax;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Fax;
using Content.Shared.Fax.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Station.Components;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Nebula;

[TestFixture]
public sealed class FaxSenderNameTest
{
    [Test]
    public async Task ReceivedFaxKnowsSender()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entMan = server.EntMan;
        var fax = entMan.System<FaxSystem>();
        var slots = entMan.System<ItemSlotsSystem>();
        var power = entMan.System<SharedPowerReceiverSystem>();

        EntityUid sender = default;
        EntityUid target = default;

        await server.WaitPost(() =>
        {
            sender = entMan.SpawnEntity("FaxMachineBase", map.GridCoords);
            target = entMan.SpawnEntity("FaxMachineCentcom", map.GridCoords);
            power.SetNeedsPower(sender, false);
            power.SetNeedsPower(target, false);
            entMan.GetComponent<FaxMachineComponent>(sender).FaxName = "Мостик";
        });

        await pair.RunTicksSync(5);
        await server.WaitPost(() => fax.Refresh(sender));

        await pair.RunTicksSync(10);

        await server.WaitAssertion(() =>
        {
            var comp = entMan.GetComponent<FaxMachineComponent>(sender);
            Assert.That(comp.KnownFaxes, Is.Not.Empty);

            var targetAddress = entMan.GetComponent<Content.Shared.DeviceNetwork.Components.DeviceNetworkComponent>(target).Address;
            fax.SetDestination(sender, targetAddress, comp);

            var paper = entMan.SpawnEntity("Paper", map.GridCoords);
            Assert.That(slots.TryInsert(sender, comp.PaperSlot, paper, null));

            fax.Send(sender, comp, new FaxSendMessage());
        });

        await pair.RunTicksSync(10);

        await server.WaitAssertion(() =>
        {
            var queue = entMan.GetComponent<FaxMachineComponent>(target).PrintingQueue;
            Assert.That(queue, Has.Count.EqualTo(1));
            Assert.That(queue.Peek().SenderFaxName, Is.EqualTo("Мостик"));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AutoPagerLinksOnlyStationFaxes()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var stationMap = await pair.CreateTestMap();
        var otherMap = await pair.CreateTestMap();
        var entMan = server.EntMan;
        var stations = entMan.System<ServerStationSystem>();
        var verbs = entMan.System<SharedVerbSystem>();
        var loc = server.ResolveDependency<ILocalizationManager>();

        await server.WaitAssertion(() =>
        {
            var station = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
            entMan.AddComponent<StationDataComponent>(station);
            stations.AddGridToStation(station, stationMap.Grid);

            var stationFax = entMan.SpawnEntity("FaxMachineBase", stationMap.GridCoords);
            entMan.SpawnEntity("FaxMachineBase", otherMap.GridCoords);

            var pager = entMan.SpawnEntity("FaxPagerAdminAuto", stationMap.GridCoords);
            var comp = entMan.GetComponent<FaxPagerComponent>(pager);
            Assert.That(comp.Faxes, Is.EqualTo(new[] { stationFax }));

            var user = entMan.SpawnEntity("MobHuman", stationMap.GridCoords);
            var unlink = verbs.GetLocalVerbs(pager, user, typeof(Verb))
                .Where(v => v.Category?.Text == loc.GetString("fax-pager-verb-unlink"))
                .ToList();
            Assert.That(unlink, Has.Count.EqualTo(1));

            unlink[0].Act!.Invoke();
            Assert.That(comp.Faxes, Is.Empty);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AutoPagerOffStationLinksOwnGridFaxes()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var gridMap = await pair.CreateTestMap();
        var otherMap = await pair.CreateTestMap();
        var entMan = server.EntMan;

        await server.WaitAssertion(() =>
        {
            var gridFax = entMan.SpawnEntity("FaxMachineBase", gridMap.GridCoords);
            entMan.SpawnEntity("FaxMachineBase", otherMap.GridCoords);

            var pager = entMan.SpawnEntity("FaxPagerAdminAuto", gridMap.GridCoords);
            Assert.That(entMan.GetComponent<FaxPagerComponent>(pager).Faxes, Is.EqualTo(new[] { gridFax }));

            var lost = entMan.SpawnEntity("FaxPagerAdminAuto", MapCoordinates.Nullspace);
            Assert.That(entMan.GetComponent<FaxPagerComponent>(lost).Faxes, Is.Empty);
        });

        await pair.CleanReturnAsync();
    }
}
