// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Atmos.Components;
using Content.Shared._Nebula.ERP.Effects;
using Content.Shared.EntityEffects;
using Content.Shared.Inventory;

namespace Content.Server._Nebula.ERP.Effects;

public sealed partial class StripClothingEntityEffectSystem : EntityEffectSystem<InventoryComponent, StripClothing>
{
    [Dependency] private InventorySystem _inventory = default!;

    protected override void Effect(Entity<InventoryComponent> entity, ref EntityEffectEvent<StripClothing> args)
    {
        foreach (var slot in args.Effect.Slots)
        {
            if (!_inventory.TryGetSlotEntity(entity, slot, out var item, entity.Comp)
                || HasComp<PressureProtectionComponent>(item))
                continue;

            _inventory.TryUnequip(entity, slot, force: true, inventory: entity.Comp);
        }
    }
}
