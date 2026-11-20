// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Server/_Goobstation/Sandevistan/SandevistanSystem.cs

using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Shared._LuaM.Sandevistan;

public sealed partial class CnsSandevistanActiveSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<CnsSandevistanActiveComponent> ent, ref ComponentStartup args)
    {
        _movement.RefreshMovementSpeedModifiers(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CnsSandevistanActiveComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent))
            _movement.RefreshMovementSpeedModifiers(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnState(Entity<CnsSandevistanActiveComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _movement.RefreshMovementSpeedModifiers(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnRefreshSpeed(Entity<CnsSandevistanActiveComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.LifeStage <= ComponentLifeStage.Running)
            args.ModifySpeed(ent.Comp.MovementSpeedModifier, ent.Comp.MovementSpeedModifier);
    }

    [SubscribeLocalEvent]
    private void OnMeleeAttack(Entity<CnsSandevistanActiveComponent> ent, ref MeleeAttackEvent args)
    {
        if (!TryComp<MeleeWeaponComponent>(args.Weapon, out var weapon))
            return;

        // weapon.AttackRate breaks things when multiple systems modify NextAttack.
        var rate = weapon.NextAttack - _timing.CurTime;
        weapon.NextAttack -= rate - rate / ent.Comp.AttackSpeedModifier;
    }
}
