// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Body;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Trauma.Shared._Nebula.Implants.Cyberpunk;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._Nebula;

/// <summary>
/// Cyberpunk implants refuse incompatible neighbours, give their passives and share cooldown changes.
/// </summary>
[TestFixture]
[TestOf(typeof(CyberwareCompatibilitySystem))]
public sealed class CyberpunkImplantsTest
{
    private static readonly EntProtoId Mob = "MobHuman";
    private static readonly ProtoId<OrganCategoryPrototype> Head = "Head";
    private static readonly ProtoId<OrganCategoryPrototype> Torso = "Torso";

    [Test]
    public async Task ImplantsWork()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var server = pair.Server;
        var entMan = server.EntMan;

        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            List<string> errors = [];
            var body = entMan.System<BodySystem>();
            var part = entMan.System<BodyPartSystem>();
            var compat = entMan.System<CyberwareCompatibilitySystem>();
            var cooldown = entMan.System<ImplantCooldownSystem>();
            var actions = entMan.System<SharedActionsSystem>();
            var timing = IoCManager.Resolve<IGameTiming>();

            var mob = entMan.SpawnAtPosition(Mob, map.GridCoords);

            EntityUid Install(EntProtoId proto, ProtoId<OrganCategoryPrototype> partCategory)
            {
                var organ = entMan.SpawnAtPosition(proto, map.GridCoords);
                if (body.GetOrgan(mob, partCategory) is not { } parent || body.GetCategory(organ) is not { } category)
                {
                    errors.Add($"{proto}: no {partCategory} or organ category.");
                    return organ;
                }

                part.TryAddSlot(parent, category);
                if (!part.InsertOrgan(parent, organ))
                    errors.Add($"{proto}: could not be inserted.");

                return organ;
            }

            void ExpectBlocked(EntProtoId proto, string why)
            {
                if (compat.CanInstall(mob, proto, out _))
                    errors.Add($"{proto} can be installed {why}.");
            }

            ExpectBlocked("HeadImplantNewton", "without a mindshield");

            Install("HeadImplantQuantumRegulator", Head);
            ExpectBlocked("HeadImplantAxolotl", "next to the quantum regulator");
            if (!entMan.HasComponent<ImplantCooldownModifierComponent>(mob))
                errors.Add("Quantum regulator gives no cooldown modifier.");

            Install("OrganDermalChitin", Torso);
            ExpectBlocked("ChestImplantRareBird", "next to the chitin");

            Install("ChestImplantAdrenaline", Torso);
            ExpectBlocked("StimulantImplant", "next to the adrenaline implant");

            var stamina = entMan.GetComponent<StaminaComponent>(mob);
            var threshold = stamina.CritThreshold;
            Install("ChestImplantIsometricStabilizer", Torso);
            if (!MathHelper.CloseTo(stamina.CritThreshold, threshold + 25f))
                errors.Add($"Stamina threshold went from {threshold} to {stamina.CritThreshold}, expected +25.");

            var panel = Install("HeadImplantFacePanel", Head);
            cooldown.StartCooldown(panel, TimeSpan.FromSeconds(100));
            cooldown.ReduceRemaining(mob, 0.5f);
            if (!cooldown.IsOnCooldown(panel))
                errors.Add("Face panel is not on cooldown.");

            if (entMan.TryGetComponent<ActionsContainerComponent>(panel, out var container))
            {
                foreach (var uid in container.Container.ContainedEntities)
                {
                    if (actions.GetAction(uid) is not { Comp.Cooldown: { } cd })
                        continue;

                    var remaining = (cd.End - timing.CurTime).TotalSeconds;
                    if (Math.Abs(remaining - 50) > 1)
                        errors.Add($"Face panel cooldown left {remaining:0.#} s after a 50% cut, expected 50 s.");
                }
            }
            else
                errors.Add("Face panel has no actions.");

            Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        });

        await pair.CleanReturnAsync();
    }
}
