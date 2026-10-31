// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Trauma.Common.Movement;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Nebula.Clothing.GearRustle;

public sealed partial class GearRustleSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnFootStep(Entity<InventoryComponent> ent, ref FootStepEvent args)
    {
        if (!_timing.IsFirstTimePredicted
            || !TryComp<InputMoverComponent>(ent, out var mover)
            || !mover.Sprinting
            || !_inventory.TryGetSlotEntity(ent, "belt", out var belt)
            || !TryComp<GearRustleComponent>(belt, out var rustle)
            || rustle.Collection is not { } collection)
            return;

        var files = _proto.Index(collection).PickFiles;
        if (files.Count == 0)
            return;

        var path = files[rustle.NextIndex % files.Count];
        rustle.NextIndex = (rustle.NextIndex + 1) % files.Count;

        var user = CompOrNull<MovementRelayTargetComponent>(ent)?.Source ?? ent.Owner;
        _audio.PlayPredicted(new SoundPathSpecifier(path), ent, user, rustle.Params);
    }
}
