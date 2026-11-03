// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/93847a07a4b0b5890003c074e4768c5ba828543a/Content.Arcane.Shared/Medical/Concussion/SharedConcussionSystem.cs

using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Damage.Components;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Arcane.Medical.Concussion;

public abstract partial class SharedConcussionSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] protected MovementSpeedModifierSystem Movement = default!;
    [Dependency] protected IGameTiming Timing = default!;

    public void AddConcussionDamage(Entity<ConcussionThresholdComponent> ent, FixedPoint2 damage)
    {
        if (damage <= 0 || HasComp<GodmodeComponent>(ent))
            return;

        var attempt = new BeforeConcussionDamageEvent(damage);
        RaiseLocalEvent(ent, ref attempt);
        if (!attempt.Cancelled && TryComp<InventoryComponent>(ent, out var inventory))
            _inventory.RelayEvent((ent.Owner, inventory), ref attempt);
        if (attempt.Cancelled || attempt.Damage <= 0)
            return;

        SetDamage(ent, ent.Comp.StoredDamage + attempt.Damage);
        ent.Comp.NextUpdate = Timing.CurTime + ent.Comp.UpdateInterval;
    }

    public void SetDamage(Entity<ConcussionThresholdComponent> ent, FixedPoint2 damage)
    {
        var cap = ent.Comp.FallbackDamageCap;
        foreach (var (threshold, state) in ent.Comp.Thresholds)
        {
            if (state != ConcussionState.Overwhelmed)
                continue;
            cap = threshold;
            break;
        }

        damage = FixedPoint2.Clamp(damage, 0, FixedPoint2.Max(0, cap));
        if (ent.Comp.StoredDamage != damage)
        {
            ent.Comp.StoredDamage = damage;
            DirtyField(ent, ent.Comp, nameof(ConcussionThresholdComponent.StoredDamage));
        }

        // Nebula start
        if (damage > 0)
            EnsureComp<ActiveConcussionComponent>(ent);
        else
            RemComp<ActiveConcussionComponent>(ent);
        // Nebula end

        var next = ConcussionState.Sane;
        foreach (var (threshold, state) in ent.Comp.Thresholds)
        {
            if (damage < threshold)
                break;
            if (ent.Comp.AllowedStates.Contains(state))
                next = state;
        }

        if (ent.Comp.CurrentState == next)
            return;
        var previous = ent.Comp.CurrentState;
        ent.Comp.CurrentState = next;
        DirtyField(ent, ent.Comp, nameof(ConcussionThresholdComponent.CurrentState));
        Movement.RefreshMovementSpeedModifiers(ent.Owner);
        var changed = new ConcussionStateChangedEvent(previous, next);
        RaiseLocalEvent(ent, ref changed);
    }

    [SubscribeLocalEvent]
    private void OnSpeed(Entity<ConcussionThresholdComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var closest = ConcussionState.Invalid;
        var multiplier = 1f;
        foreach (var (state, speed) in ent.Comp.SpeedModifierThresholds)
        {
            if (state <= ent.Comp.CurrentState && state > closest)
            {
                closest = state;
                multiplier = Math.Max(0, speed);
            }
        }
        args.ModifySpeed(multiplier, multiplier);
    }

    [SubscribeLocalEvent]
    private void OnResistance(Entity<ConcussionResistanceComponent> ent, ref InventoryRelayedEvent<BeforeConcussionDamageEvent> args)
    {
        args.Args.Damage *= 1f - Math.Clamp(ent.Comp.Resistance, 0f, 1f);
        if (args.Args.Damage <= 0)
            args.Args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnExamine(Entity<ConcussionResistanceComponent> ent, ref ExaminedEvent args)
    {
        var resistance = Math.Clamp(ent.Comp.Resistance, 0f, 1f);
        if (resistance > 0)
            args.PushMarkup(Loc.GetString("concussion-resistance-percent", ("percent", MathF.Round(resistance * 100f)))); // Nebula edit
    }

    public static float GetIntensity(ConcussionThresholdComponent comp)
    {
        if (comp.CurrentState == ConcussionState.Sane)
            return 0f;
        var minor = 0f;
        var overwhelmed = 0f;
        foreach (var (threshold, state) in comp.Thresholds)
        {
            if (state == ConcussionState.Minor)
                minor = threshold.Float();
            else if (state == ConcussionState.Overwhelmed)
                overwhelmed = threshold.Float();
        }
        var range = overwhelmed - minor;
        return range <= 0 ? 1f : Math.Clamp((comp.StoredDamage.Float() - minor) / range, 0f, 1f);
    }
}
