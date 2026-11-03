// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Trauma.Common.Movement;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._Nebula.Clothing.GearRustle;

public sealed partial class GearRustleSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private InventorySystem _inventory = default!;

    [SubscribeLocalEvent]
    private void OnFootStep(Entity<InventoryComponent> ent, ref FootStepEvent args)
    {
        if (!TryComp<InputMoverComponent>(ent, out var mover)
            || !mover.Sprinting
            || !_inventory.TryGetSlotEntity(ent, "belt", out var belt)
            || !TryComp<GearRustleComponent>(belt, out var rustle))
            return;

        var user = CompOrNull<MovementRelayTargetComponent>(ent)?.Source ?? ent.Owner;
        _audio.PlayPredicted(rustle.Sound, ent, user);
    }
}
