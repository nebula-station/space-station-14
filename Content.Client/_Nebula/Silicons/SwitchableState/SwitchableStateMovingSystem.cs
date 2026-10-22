// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Silicons.SwitchableState;
using Content.Shared.Movement.Events;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.Silicons.SwitchableState;

public sealed partial class SwitchableStateMovingSystem : EntitySystem
{
    [Dependency] private SpriteSystem _spriteSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [SubscribeLocalEvent]
    private void OnMovementUpdate(Entity<BorgSwitchableStateComponent> ent, ref AfterSpriteMovementUpdateEvent args)
    {
        if (ent.Comp.CurrentType != BorgStateType.Base || ent.Comp.BaseSprite is not SpriteSpecifier.Rsi rsi)
            return;

        if (!TryComp<BorgChassisComponent>(ent, out var chassis) || !TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!_appearance.TryGetData<bool>(ent, BorgVisuals.HasPlayer, out var hasPlayer) || !hasPlayer)
            return;

        if (!_spriteSystem.TryGetLayer((ent, sprite), BorgVisualLayers.Light, out var layer, false))
            return;

        var movingState = rsi.RsiState + "_moving_e";
        if (args.IsMoving && layer.ActualRsi?.TryGetState(movingState, out _) == true)
            _spriteSystem.LayerSetRsiState((ent, sprite), BorgVisualLayers.Light, movingState);
        else
            _spriteSystem.LayerSetRsiState((ent, sprite), BorgVisualLayers.Light, chassis.HasMindState);
    }
}
