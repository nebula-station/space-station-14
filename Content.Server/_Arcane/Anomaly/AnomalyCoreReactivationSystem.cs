// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/02b6d617ada28d2a78cfd227b9860838fb082909/Content.Arcane.Server/Anomaly/AnomalyCoreReactivationSystem.cs

using Content.Server.Atmos.EntitySystems;
using Content.Server.Electrocution;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Arcane.Anomaly;

/// <summary>
/// Turns an inert anomaly core back into an active one when a reactivation reagent is put into it.
/// Hazardous reagents may hurt nearby players or release the anomaly instead.
/// </summary>
public sealed partial class AnomalyCoreReactivationSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private ElectrocutionSystem _electrocution = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private SharedAnomalyCoreSystem _core = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;

    private const string SolutionName = "reactivation";
    private const float HazardRange = 3f;
    private const string InertSuffix = "Inert";

    private readonly HashSet<EntityUid> _processing = [];

    [SubscribeLocalEvent]
    private void OnSolutionChanged(Entity<AnomalyCoreComponent> ent, ref SolutionChangedEvent args)
    {
        if (args.Solution.Comp.Id != SolutionName || !ent.Comp.IsDecayed || !_processing.Add(ent))
            return;

        try
        {
            TryReactivate(ent, args.Solution);
        }
        finally
        {
            _processing.Remove(ent);
        }
    }

    private void TryReactivate(Entity<AnomalyCoreComponent> ent, Entity<SolutionComponent> solution)
    {
        // A core with nothing to turn back into keeps its reagents.
        if (GetReactivationPrototype(ent) is not { } active)
            return;

        if (TryConsume(ent.Comp.ReactivationReagents, ent.Comp.ReactivationReagentAmount, solution))
        {
            Reactivate(ent, active);
            return;
        }

        if (TryConsume(ent.Comp.HazardousReactivationReagents, ent.Comp.ReactivationReagentAmount, solution))
        {
            if (_random.Prob(ent.Comp.HazardousFailureChance))
                DoHazardousFailure(ent);
            else
                Reactivate(ent, active);
            return;
        }

        // Full of something useless: reject it.
        if (solution.Comp.Solution.AvailableVolume > 0)
            return;

        _solutions.RemoveAllSolution(solution);
        _popup.PopupEntity(Loc.GetString("anomaly-core-reactivation-failed"), ent, PopupType.SmallCaution);
    }

    private bool TryConsume(List<ProtoId<ReagentPrototype>> reagents, FixedPoint2 amount, Entity<SolutionComponent> solution)
    {
        foreach (var reagent in reagents)
        {
            var id = new ReagentId(reagent, null);
            if (solution.Comp.Solution.GetReagentQuantity(id) < amount)
                continue;

            _solutions.RemoveReagent(solution, id, amount);
            return true;
        }

        return false;
    }

    private void Reactivate(Entity<AnomalyCoreComponent> ent, EntProtoId active)
    {
        var reactivated = SpawnAtPosition(active, Transform(ent).Coordinates);
        _core.Reactivate(ent, reactivated);
        _popup.PopupEntity(Loc.GetString("anomaly-core-reactivated"), reactivated, PopupType.Medium);
    }

    /// <summary>
    /// The active core for this inert one: the remembered prototype, or the same id without the "Inert" suffix.
    /// </summary>
    private EntProtoId? GetReactivationPrototype(Entity<AnomalyCoreComponent> ent)
    {
        if (ent.Comp.ReactivationPrototype is { } proto)
            return proto;

        if (MetaData(ent).EntityPrototype?.ID is not { } id || !id.EndsWith(InertSuffix))
            return null;

        var active = id[..^InertSuffix.Length];
        return _proto.HasIndex<EntityPrototype>(active) ? active : null;
    }

    private void DoHazardousFailure(Entity<AnomalyCoreComponent> ent)
    {
        _popup.PopupEntity(Loc.GetString("anomaly-core-reactivation-hazard"), ent, PopupType.MediumCaution);

        List<EntityUid> targets = [];
        foreach (var uid in _lookup.GetEntitiesInRange<ActorComponent>(Transform(ent).Coordinates, HazardRange))
        {
            targets.Add(uid);
        }

        var applied = _random.Next(4) switch
        {
            0 => DamageTargets(targets, ent),
            1 => IgniteTargets(targets, ent),
            2 => ElectrocuteTargets(targets, ent),
            _ => false,
        };

        if (!applied)
            SpawnHazardousAnomaly(ent);
    }

    private bool DamageTargets(List<EntityUid> targets, EntityUid core)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Heat", 15);
        damage.DamageDict.Add("Poison", 10);

        var applied = false;
        foreach (var target in targets)
        {
            if (HasComp<DamageableComponent>(target))
                applied |= _damageable.TryChangeDamage(target, damage, origin: core);
        }

        return applied;
    }

    private bool IgniteTargets(List<EntityUid> targets, EntityUid core)
    {
        var applied = false;
        foreach (var target in targets)
        {
            if (!HasComp<FlammableComponent>(target))
                continue;

            _flammable.Ignite(target, core);
            applied = true;
        }

        return applied;
    }

    private bool ElectrocuteTargets(List<EntityUid> targets, EntityUid core)
    {
        var applied = false;
        foreach (var target in targets)
        {
            applied |= _electrocution.TryDoElectrocution(target, core, 30, TimeSpan.FromSeconds(2), true);
        }

        return applied;
    }

    private void SpawnHazardousAnomaly(Entity<AnomalyCoreComponent> ent)
    {
        if (ent.Comp.HazardousAnomalyPrototype is not { } anomaly)
            return;

        SpawnAtPosition(anomaly, Transform(ent).Coordinates);
        QueueDel(ent);
    }
}
