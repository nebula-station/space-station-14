// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Movement.Systems;

namespace Content.Shared._Nebula.Implants;

public sealed partial class CyberHeartEmpSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movement = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<CyberHeartEmpComponent> ent, ref ComponentStartup args)
    {
        _movement.RefreshMovementSpeedModifiers(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CyberHeartEmpComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent.Owner))
            _movement.RefreshMovementSpeedModifiers(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnRefreshSpeed(Entity<CyberHeartEmpComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.LifeStage <= ComponentLifeStage.Running)
            args.ModifySpeed(ent.Comp.SpeedModifier);
    }
}
