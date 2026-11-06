// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Wieldable.Components;

namespace Content.Shared._Nebula.Tackle;

public sealed partial class TackleWieldedDropSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    /// <summary>
    /// Whether the knockdown from a tackle should drop everything the target holds.
    /// With <see cref="TackleWieldedDropComponent"/> gloves it drops the wielded items itself and returns false.
    /// </summary>
    public bool ShouldDrop(EntityUid user, EntityUid target, bool drop)
    {
        if (!drop
            || !_inventory.TryGetSlotEntity(user, "gloves", out var gloves)
            || !TryComp<TackleWieldedDropComponent>(gloves, out var wieldedDrop)
            || !wieldedDrop.Enabled)
            return drop;

        List<EntityUid> wielded = [];
        foreach (var held in _hands.EnumerateHeld(target))
        {
            if (TryComp<WieldableComponent>(held, out var wieldable) && wieldable.Wielded)
                wielded.Add(held);
        }

        foreach (var item in wielded)
        {
            _hands.TryDrop(target, item, checkActionBlocker: false);
        }

        return false;
    }
}
