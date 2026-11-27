// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._LuaM.Cyberware;
using Content.Shared._Nebula.Implants;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Mobs;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

/// <summary>
/// Cooldowns of cyberware: their actions and the implants that keep their own timer.
/// </summary>
public sealed partial class ImplantCooldownSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private CyberwareCompatibilitySystem _compat = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private IGameTiming _timing = default!;

    #region Handle events

    [SubscribeLocalEvent]
    private void OnActionAdded(Entity<CyberwareHumanityComponent> ent, ref ActionAddedEvent args)
    {
        EnsureComp<ImplantActionComponent>(args.Action);
    }

    [SubscribeLocalEvent]
    private void OnActionPerformed(Entity<ImplantActionComponent> ent, ref ActionPerformedEvent args)
    {
        if (_actions.GetAction(ent.Owner) is not { } action
            || action.Comp.Container is not { } implant
            || action.Comp.Cooldown is not { } cooldown)
            return;

        var length = cooldown.End - cooldown.Start;
        var modified = ModifyCooldown(args.Performer, implant, length);
        if (modified != length)
            _actions.SetCooldown((action.Owner, action.Comp), cooldown.Start, cooldown.Start + modified);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.OldMobState != MobState.Alive
            || args.NewMobState is not (MobState.SoftCrit or MobState.Critical)
            || args.Origin is not { } origin
            || GetAttacker<CritCooldownRefundComponent>(origin) is not { } attacker
            || attacker.Owner == args.Target)
            return;

        ReduceRemaining(attacker, attacker.Comp.Fraction);
    }

    [SubscribeLocalEvent]
    private void OnRebooterQuery(Entity<CnsRebooterComponent> ent, ref ImplantCooldownQueryEvent args)
    {
        args.OnCooldown |= _timing.CurTime < ent.Comp.NextUse;
    }

    [SubscribeLocalEvent]
    private void OnRebooterReduce(Entity<CnsRebooterComponent> ent, ref ImplantCooldownReduceEvent args)
    {
        var now = _timing.CurTime;
        if (ent.Comp.NextUse <= now)
            return;

        ent.Comp.NextUse -= (ent.Comp.NextUse - now) * args.Fraction;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnRebooterGetCooldown(Entity<CnsRebooterComponent> ent, ref GetImplantCooldownEvent args)
    {
        args.Cooldown = ModifyCooldown(args.Body, ent, args.Cooldown);
    }

    [SubscribeLocalEvent]
    private void OnRebooterReset(Entity<CnsRebooterComponent> ent, ref ImplantCooldownResetEvent args)
    {
        ent.Comp.NextUse = _timing.CurTime;
        Dirty(ent);
    }

    #endregion

    #region API

    /// <summary>
    /// Applies the cooldown modifiers of the body to a new cooldown of its implant.
    /// </summary>
    public TimeSpan ModifyCooldown(EntityUid body, EntityUid implant, TimeSpan cooldown)
    {
        if (TryComp<ImplantCooldownModifierComponent>(body, out var modifier)
            && !HasComp<QuantumRegulatorComponent>(implant))
            cooldown *= modifier.Multiplier;

        if (RemComp<ImplantDoubleCooldownComponent>(implant))
            cooldown *= 2;

        return cooldown;
    }

    /// <summary>
    /// Takes the given part off the remaining cooldown of every implant in the body.
    /// </summary>
    public void ReduceRemaining(EntityUid body, float fraction)
    {
        var now = _timing.CurTime;
        foreach (var implant in _compat.GetInstalled(body))
        {
            foreach (var action in GetActions(implant))
            {
                if (action.Comp.Cooldown is not { } cooldown || cooldown.End <= now)
                    continue;

                _actions.SetCooldown((action.Owner, action.Comp), cooldown.Start, cooldown.End - (cooldown.End - now) * fraction);
            }

            var ev = new ImplantCooldownReduceEvent(fraction);
            RaiseLocalEvent(implant, ref ev);
        }
    }

    /// <summary>
    /// Whether any action of the implant is cooling down, or its own timer is not ready.
    /// </summary>
    public bool IsOnCooldown(EntityUid implant)
    {
        var now = _timing.CurTime;
        foreach (var action in GetActions(implant))
        {
            if (action.Comp.Cooldown is { } cooldown && cooldown.End > now)
                return true;
        }

        var ev = new ImplantCooldownQueryEvent();
        RaiseLocalEvent(implant, ref ev);
        return ev.OnCooldown;
    }

    /// <summary>
    /// Makes the implant ready at once. Its next cooldown lasts twice as long.
    /// </summary>
    public void ResetCooldown(EntityUid implant)
    {
        foreach (var action in GetActions(implant))
        {
            _actions.RemoveCooldown((action.Owner, action.Comp));
        }

        var ev = new ImplantCooldownResetEvent();
        RaiseLocalEvent(implant, ref ev);

        EnsureComp<ImplantDoubleCooldownComponent>(implant);
    }

    /// <summary>
    /// Puts every action of the implant on the given cooldown.
    /// </summary>
    public void StartCooldown(EntityUid implant, TimeSpan cooldown)
    {
        foreach (var action in GetActions(implant))
        {
            _actions.SetCooldown((action.Owner, action.Comp), cooldown);
        }
    }

    /// <summary>
    /// Implants of the body that are recharging and could be reset.
    /// </summary>
    public IEnumerable<EntityUid> GetCoolingImplants(EntityUid body)
    {
        foreach (var implant in _compat.GetInstalled(body))
        {
            if (!HasComp<QuantumRegulatorComponent>(implant) && IsOnCooldown(implant))
                yield return implant;
        }
    }

    /// <summary>
    /// Finds who is behind an attack: the origin itself, or whoever holds the weapon that is the origin.
    /// </summary>
    public Entity<T>? GetAttacker<T>(EntityUid origin) where T : IComponent
    {
        if (TryComp<T>(origin, out var comp))
            return (origin, comp);

        if (_container.TryGetContainingContainer(origin, out var container)
            && TryComp(container.Owner, out comp))
            return (container.Owner, comp);

        return null;
    }

    #endregion

    private IEnumerable<Entity<ActionComponent>> GetActions(EntityUid implant)
    {
        if (!TryComp<ActionsContainerComponent>(implant, out var container))
            yield break;

        foreach (var uid in container.Container.ContainedEntities)
        {
            if (HasComp<ImplantActionComponent>(uid) && _actions.GetAction(uid, false) is { } action)
                yield return action;
        }
    }
}
