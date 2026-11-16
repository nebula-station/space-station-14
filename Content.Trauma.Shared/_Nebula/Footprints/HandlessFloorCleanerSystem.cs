// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Decals;
using Content.Shared.Fluids;
using Content.Shared.Interaction;
using Content.Trauma.Shared.Footprints;

namespace Content.Trauma.Shared._Nebula.Footprints;

/// <summary>
/// Lets a handless cleaner, like a cleanbot, wipe the decals it was sent to.
/// </summary>
public sealed partial class HandlessFloorCleanerSystem : EntitySystem
{
    [Dependency] private FloorCleanerSystem _floorCleaner = default!;

    [SubscribeLocalEvent(before: [typeof(SharedAbsorbentSystem)])]
    private void OnActivateInWorld(Entity<FloorCleanerComponent> ent, ref UserActivateInWorldEvent args)
    {
        if (args.Handled || !HasComp<DecalComponent>(args.Target))
            return;

        args.Handled = _floorCleaner.CleanDecals(ent, Transform(args.Target).Coordinates, args.User);
    }
}
