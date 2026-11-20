// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Shared/_LuaM/Cyberware/CyberwareSpeedModifierSystem.cs

using Content.Shared.Movement.Systems;

namespace Content.Shared._LuaM.Cyberware;

public sealed partial class CyberwareSpeedModifierSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movement = default!;

    [SubscribeLocalEvent]
    private void OnRefresh(Entity<CyberwareSpeedModifierComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        // the shutdown refresh must not count the component that is going away
        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.ModifySpeed(ent.Comp.WalkModifier, ent.Comp.SprintModifier);
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<CyberwareSpeedModifierComponent> ent, ref ComponentStartup args)
    {
        _movement.RefreshMovementSpeedModifiers(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CyberwareSpeedModifierComponent> ent, ref ComponentShutdown args)
    {
        _movement.RefreshMovementSpeedModifiers(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnState(Entity<CyberwareSpeedModifierComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _movement.RefreshMovementSpeedModifiers(ent.Owner);
    }
}
