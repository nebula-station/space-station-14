// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.IntegrationTests.Pair;
using Content.Server._Orion.Bitrunning.Systems;
using Content.Server.Power.Components;
using Content.Shared._Orion.Bitrunning;
using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared._Orion.Bitrunning.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Roles;
using Robust.Shared.ContentPack;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Nebula.Bitrunning;

/// <summary>
/// Validates bitrunning domains and runs a runner through a domain: connect, die, stop and reboot.
/// </summary>
[TestFixture]
[TestOf(typeof(QuantumServerSystem))]
public sealed class BitrunningTest
{
    private const string ServerProto = "QuantumServer";
    private const string NetpodProto = "Netpod";
    private const string RunnerProto = "MobHuman";
    private const string Domain = "BeachBar";
    private const string ServerSourcePort = "BitrunningServerSource";
    private const string NetpodSinkPort = "BitrunningNetpodSink";
    private const string BodyContainer = "netpod-body";

    private static readonly TimeSpan TestCooldown = TimeSpan.FromSeconds(1);

    [Test]
    public async Task DomainPrototypesAreValid()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoMan = server.ProtoMan;
        var resMan = server.ResolveDependency<IResourceManager>();
        var loc = server.ResolveDependency<ILocalizationManager>();

        await server.WaitAssertion(() =>
        {
            List<string> errors = [];

            var domains = protoMan.EnumeratePrototypes<BitrunningVirtualDomainPrototype>().OrderBy(d => d.ID).ToList();
            Assert.That(domains, Is.Not.Empty, "No bitrunningVirtualDomain prototypes found.");

            foreach (var domain in domains)
            {
                if (!resMan.ContentFileExists(domain.MapPath))
                    errors.Add($"Domain '{domain.ID}': map '{domain.MapPath}' does not exist.");

                foreach (var key in (string[]) [domain.Name, domain.Description])
                {
                    if (!loc.HasString(key))
                        errors.Add($"Domain '{domain.ID}': missing localization '{key}'.");
                }

                if (domain.ForcedLoadout is { } loadout && !protoMan.HasIndex<StartingGearPrototype>(loadout))
                    errors.Add($"Domain '{domain.ID}': unknown forced loadout '{loadout}'.");

                if (domain.ObjectiveTypePool.Contains(BitrunningObjectiveType.None))
                    errors.Add($"Domain '{domain.ID}': objective pool contains None.");
            }

            Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task RunnerConnectsAndReturnsOnStop()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var server = pair.Server;
        var entMan = server.EntMan;
        var quantum = entMan.System<QuantumServerSystem>();

        var map = await pair.CreateTestMap();
        var (serverUid, podUid) = await SpawnMachines(pair, map.GridCoords);
        var (runner, mindId) = await SpawnRunner(pair, map.GridCoords);

        EntityUid? domainMap = null;
        await server.WaitAssertion(() =>
        {
            var serverComp = entMan.GetComponent<QuantumServerComponent>(serverUid);
            Assert.That(quantum.TryColdBoot(serverUid, Domain), "Domain failed to boot.");
            Assert.That(serverComp.State, Is.EqualTo(BitrunningServerState.Running));
            domainMap = serverComp.DomainMapUid;
            Assert.That(domainMap, Is.Not.Null);
        });

        await InsertRunner(pair, podUid, runner);

        await server.WaitAssertion(() =>
        {
            var pod = entMan.GetComponent<NetpodComponent>(podUid);
            var mind = entMan.GetComponent<MindComponent>(mindId);
            Assert.That(pod.Avatar, Is.Not.Null, "Runner was not connected on entering the netpod.");
            Assert.That(mind.OwnedEntity, Is.EqualTo(pod.Avatar), "Runner's mind is not in the avatar.");
            Assert.That(entMan.HasComponent<AvatarConnectionComponent>(pod.Avatar!.Value));

            var serverComp = entMan.GetComponent<QuantumServerComponent>(serverUid);
            Assert.That(quantum.StopDomain((serverUid, serverComp)), "Running domain failed to stop.");
            Assert.That(serverComp.State, Is.EqualTo(BitrunningServerState.CoolingDown));
            Assert.That(mind.OwnedEntity, Is.EqualTo(runner), "Runner's mind did not return to the body.");

            Assert.That(quantum.StopDomain((serverUid, serverComp)), Is.False, "Stop during cooldown must be ignored.");
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.Deleted(domainMap!.Value), "Domain map was not deleted after stop.");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CooldownDoesNotResetRebootedDomain()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var server = pair.Server;
        var entMan = server.EntMan;
        var quantum = entMan.System<QuantumServerSystem>();

        var map = await pair.CreateTestMap();
        var (serverUid, _) = await SpawnMachines(pair, map.GridCoords);

        await server.WaitAssertion(() =>
        {
            var serverComp = entMan.GetComponent<QuantumServerComponent>(serverUid);
            Assert.That(quantum.TryColdBoot(serverUid, Domain));
            Assert.That(quantum.StopDomain((serverUid, serverComp)));
        });

        // A second stop halfway through the cooldown used to schedule another reset timer.
        await pair.RunSeconds((float) TestCooldown.TotalSeconds / 2);
        await server.WaitPost(() =>
        {
            var serverComp = entMan.GetComponent<QuantumServerComponent>(serverUid);
            quantum.StopDomain((serverUid, serverComp));
        });

        // Cooldown ends are checked once a second.
        await pair.RunSeconds((float) TestCooldown.TotalSeconds * 0.7f + 1f);

        await server.WaitAssertion(() =>
        {
            var serverComp = entMan.GetComponent<QuantumServerComponent>(serverUid);
            Assert.That(serverComp.State, Is.EqualTo(BitrunningServerState.Ready), "Server did not finish cooling down.");
            Assert.That(quantum.TryColdBoot(serverUid, Domain), "Domain failed to reboot after cooldown.");
        });

        await pair.RunSeconds((float) TestCooldown.TotalSeconds * 2);

        await server.WaitAssertion(() =>
        {
            var serverComp = entMan.GetComponent<QuantumServerComponent>(serverUid);
            Assert.That(serverComp.State, Is.EqualTo(BitrunningServerState.Running), "A stale cooldown timer reset the running domain.");
            Assert.That(serverComp.CurrentDomain, Is.EqualTo(Domain));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AvatarDeathDamagesRunnerOnce()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var server = pair.Server;
        var entMan = server.EntMan;
        var quantum = entMan.System<QuantumServerSystem>();
        var mobState = entMan.System<MobStateSystem>();
        var damageable = entMan.System<DamageableSystem>();

        var map = await pair.CreateTestMap();
        var (serverUid, podUid) = await SpawnMachines(pair, map.GridCoords);
        var (runner, mindId) = await SpawnRunner(pair, map.GridCoords);

        await server.WaitAssertion(() => Assert.That(quantum.TryColdBoot(serverUid, Domain)));
        await InsertRunner(pair, podUid, runner);

        await server.WaitAssertion(() =>
        {
            var avatar = entMan.GetComponent<NetpodComponent>(podUid).Avatar;
            Assert.That(avatar, Is.Not.Null, "Runner was not connected on entering the netpod.");
            var before = damageable.GetTotalDamage(runner).Float();

            // With no damage on the avatar, the runner takes the fixed fatal penalty: 20 Blunt + 2 Cellular.
            mobState.ChangeMobState(avatar!.Value, MobState.Dead);

            var mind = entMan.GetComponent<MindComponent>(mindId);
            Assert.That(mind.OwnedEntity, Is.EqualTo(runner), "Runner's mind did not return after the avatar died.");

            var taken = damageable.GetTotalDamage(runner).Float() - before;
            Assert.That(taken, Is.GreaterThan(0f), "Runner took no damage from the avatar's death.");
            Assert.That(taken, Is.LessThanOrEqualTo(22f + 0.5f), "Avatar death damage was applied to the runner more than once.");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task StopSurvivesDeletedDomainMap()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var server = pair.Server;
        var entMan = server.EntMan;
        var quantum = entMan.System<QuantumServerSystem>();

        var map = await pair.CreateTestMap();
        var (serverUid, _) = await SpawnMachines(pair, map.GridCoords);

        await server.WaitAssertion(() =>
        {
            var serverComp = entMan.GetComponent<QuantumServerComponent>(serverUid);
            Assert.That(quantum.TryColdBoot(serverUid, Domain));
            entMan.DeleteEntity(serverComp.DomainMapUid!.Value);

            Assert.DoesNotThrow(() => quantum.StopDomain((serverUid, serverComp)));
            Assert.That(serverComp.DomainMapUid, Is.Null);

            Assert.That(quantum.TryColdBoot(serverUid, Domain), Is.False, "Server must stay in cooldown.");
            Assert.DoesNotThrow(() => entMan.DeleteEntity(serverUid));
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Spawns a powered quantum server linked to a powered netpod, with a short cooldown and enough points for any domain.
    /// </summary>
    private static async Task<(EntityUid Server, EntityUid Pod)> SpawnMachines(TestPair pair, EntityCoordinates coords)
    {
        var server = pair.Server;
        var entMan = server.EntMan;
        var power = entMan.System<SharedPowerReceiverSystem>();
        var deviceLink = entMan.System<SharedDeviceLinkSystem>();

        EntityUid serverUid = default;
        EntityUid podUid = default;
        await server.WaitPost(() =>
        {
            serverUid = entMan.SpawnEntity(ServerProto, coords);
            podUid = entMan.SpawnEntity(NetpodProto, coords);

            foreach (var uid in (EntityUid[]) [serverUid, podUid])
            {
                power.SetNeedsPower(uid, false);
                entMan.GetComponent<ApcPowerReceiverComponent>(uid).Powered = true;
            }

            var serverComp = entMan.GetComponent<QuantumServerComponent>(serverUid);
            serverComp.Cooldown = TestCooldown;
            serverComp.Points = 1000;

            deviceLink.SaveLinks(null, serverUid, podUid, [(ServerSourcePort, NetpodSinkPort)]);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<NetpodComponent>(podUid).LinkedServer, Is.EqualTo(serverUid), "Netpod is not linked to the server.");
        });

        return (serverUid, podUid);
    }

    private static async Task<(EntityUid Runner, EntityUid Mind)> SpawnRunner(TestPair pair, EntityCoordinates coords)
    {
        var server = pair.Server;
        var entMan = server.EntMan;
        var minds = entMan.System<SharedMindSystem>();

        EntityUid runner = default;
        EntityUid mindId = default;
        await server.WaitPost(() =>
        {
            runner = entMan.SpawnEntity(RunnerProto, coords);
            var mind = minds.CreateMind(null);
            minds.TransferTo(mind, runner, mind: mind);
            mindId = mind.Owner;
        });

        return (runner, mindId);
    }

    private static async Task InsertRunner(TestPair pair, EntityUid podUid, EntityUid runner)
    {
        var server = pair.Server;
        var entMan = server.EntMan;
        var containers = entMan.System<SharedContainerSystem>();

        await server.WaitAssertion(() =>
        {
            var container = containers.GetContainer(podUid, BodyContainer);
            Assert.That(containers.Insert(runner, container), "Runner was not inserted into the netpod.");
        });

        await pair.RunTicksSync(5);
    }
}
