// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction;
using Content.Trauma.Shared.Tools;

namespace Content.Trauma.Shared._Nebula.Tools;

/// <summary>
/// Refuels a <see cref="SolutionRefuelComponent"/> item when the fuel tank is used on it,
/// the same as using the item on the tank.
/// </summary>
public sealed partial class SolutionRefuelReverseSystem : EntitySystem
{
    [Dependency] private SolutionRefuelSystemSystem _refuel = default!;

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<SolutionRefuelComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var ev = new AfterInteractEvent(args.User, ent.Owner, args.Used, args.ClickLocation, canReach: true);
        _refuel.OnSolutionRefuelAfterInteract(ent, ref ev);
        args.Handled = ev.Handled;
    }
}
