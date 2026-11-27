using Content.Shared.Anomaly.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Weapons.Melee.Components;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Anomaly;

/// <summary>
/// This component reduces the value of the entity during decay
/// </summary>
public sealed partial class SharedAnomalyCoreSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    // Nebula start
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    // Nebula end

    public override void Initialize()
    {
        SubscribeLocalEvent<AnomalyCoreComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CorePoweredThrowerComponent, AttemptMeleeThrowOnHitEvent>(OnAttemptMeleeThrowOnHit);
        SubscribeLocalEvent<CorePoweredThrowerComponent, ExaminedEvent>(OnCorePoweredExamined);
    }

    private void OnMapInit(Entity<AnomalyCoreComponent> core, ref MapInitEvent args)
    {
        core.Comp.DecayMoment = _gameTiming.CurTime + TimeSpan.FromSeconds(core.Comp.TimeToDecay);
        Dirty(core, core.Comp);
    }

    private void OnAttemptMeleeThrowOnHit(Entity<CorePoweredThrowerComponent> ent, ref AttemptMeleeThrowOnHitEvent args)
    {
        var (uid, comp) = ent;

        // don't waste charges on non-anchorable non-anomalous static bodies.
        if (!HasComp<AnomalyComponent>(args.Target)
            && !HasComp<AnchorableComponent>(args.Target)
            && TryComp<PhysicsComponent>(args.Target, out var body)
            && body.BodyType == BodyType.Static)
            return;

        args.Cancelled = true;
        args.Handled = true;

        if (!_itemSlots.TryGetSlot(uid, comp.CoreSlotId, out var slot))
            return;

        if (!TryComp<AnomalyCoreComponent>(slot.Item, out var coreComponent))
            return;

        if (coreComponent.IsDecayed)
        {
            if (coreComponent.Charge <= 0)
                return;
            args.Cancelled = false;
            coreComponent.Charge--;
        }
        else
        {
            args.Cancelled = false;
        }
    }

    private void OnCorePoweredExamined(Entity<CorePoweredThrowerComponent> ent, ref ExaminedEvent args)
    {
        var (uid, comp) = ent;
        if (!args.IsInDetailsRange)
            return;

        if (!_itemSlots.TryGetSlot(uid, comp.CoreSlotId, out var slot) ||
            !TryComp<AnomalyCoreComponent>(slot.Item, out var coreComponent))
        {
            args.PushMarkup(Loc.GetString("anomaly-gorilla-charge-none"));
            return;
        }

        if (coreComponent.IsDecayed)
        {
            args.PushMarkup(Loc.GetString("anomaly-gorilla-charge-limit", ("count", coreComponent.Charge)));
        }
        else
        {
            args.PushMarkup(Loc.GetString("anomaly-gorilla-charge-infinite"));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AnomalyCoreComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.IsDecayed)
                continue;

            //When time runs out, we completely decompose
            if (component.DecayMoment < _gameTiming.CurTime)
                Decay(uid, component);
        }
    }

    private void Decay(EntityUid uid, AnomalyCoreComponent component)
    {
        // Nebula start
        if (component.DecayPrototype is { } decayPrototype)
        {
            if (!_net.IsServer)
                return;

            var decayed = SpawnAtPosition(decayPrototype, Transform(uid).Coordinates);
            if (TryComp<AnomalyCoreComponent>(decayed, out var decayedCore))
            {
                decayedCore.ReactivationPrototype = MetaData(uid).EntityPrototype?.ID;
                decayedCore.HazardousAnomalyPrototype = component.HazardousAnomalyPrototype;
                Dirty(decayed, decayedCore);
            }

            ReplaceCore(uid, decayed);
            return;
        }
        // Nebula end

        _appearance.SetData(uid, AnomalyCoreVisuals.Decaying, false);
        component.IsDecayed = true;
        Dirty(uid, component);
    }

    // Nebula start
    /// <summary>
    /// Puts <paramref name="replacement"/> where <paramref name="core"/> is (hand, item slot, storage or floor) and deletes
    /// the old core. A slot holds one entity, so the old core leaves it through the container API first.
    /// </summary>
    public void ReplaceCore(EntityUid core, EntityUid replacement)
    {
        if (_container.TryGetContainingContainer(core, out var container))
        {
            _container.Remove(core, container, force: true);
            if (!_container.Insert(replacement, container))
                _transform.DropNextTo(replacement, container.Owner);
        }

        QueueDel(core);
    }

    /// <summary>
    /// Swaps an inert core for the freshly spawned active one; the active core decays back into the same inert
    /// prototype and keeps the anomaly it came from, so the cycle can repeat.
    /// </summary>
    public void Reactivate(Entity<AnomalyCoreComponent> inert, EntityUid active)
    {
        if (TryComp<AnomalyCoreComponent>(active, out var activeCore))
        {
            activeCore.DecayPrototype = MetaData(inert).EntityPrototype?.ID;
            activeCore.HazardousAnomalyPrototype = inert.Comp.HazardousAnomalyPrototype;
            Dirty(active, activeCore);
        }

        ReplaceCore(inert, active);
    }
    // Nebula end
}
