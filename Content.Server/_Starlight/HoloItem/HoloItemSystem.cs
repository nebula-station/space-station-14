// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/TetrisZz/frontier-Radiant/blob/743dfcac03/Content.Server/_Starlight/HoloItem/HoloItemSystem.cs

using Content.Shared.Interaction;
using Content.Shared.PowerCell;
using Robust.Shared.Spawners;

namespace Content.Server._Starlight.HoloItem;

public sealed partial class HoloItemSystem : EntitySystem
{
    [Dependency] private PowerCellSystem _powerCell = default!;

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<HoloItemComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled
            || !args.CanReach
            || args.Target is not { } target)
            return;

        foreach (var registration in ent.Comp.RequiredComponents.Values)
        {
            if (!HasComp(target, registration.Component.GetType()))
                return;
        }

        if (!_powerCell.TryUseCharge(ent.Owner, ent.Comp.ChargeUse, user: args.User))
            return;

        var holo = SpawnAtPosition(ent.Comp.ItemPrototype, Transform(args.User).Coordinates);
        EnsureComp<TimedDespawnComponent>(holo);
        var interact = new AfterInteractEvent(args.User, holo, target, args.ClickLocation, true);
        RaiseLocalEvent(holo, interact);
        args.Handled = true;
    }
}
