// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Silicons.SwitchableState;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.Silicons.SwitchableState;

public sealed partial class SwitchableStateVisualizerSystem : VisualizerSystem<BorgSwitchableStateComponent>
{
    [Dependency] private SpriteSystem _spriteSystem = default!;

    protected override void OnAppearanceChange(EntityUid uid, BorgSwitchableStateComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<BorgStateType>(
                uid, SwitchStateVisuals.Key, out var type, args.Component))
            return;

        if (comp.BaseSprite is not SpriteSpecifier.Rsi rsi)
            return;

        var rsiState = type == BorgStateType.Base ? rsi.RsiState
            : $"{rsi.RsiState}_{type.ToString().ToLowerInvariant()}";

        _spriteSystem.LayerSetRsiState((uid, args.Sprite), BorgVisualLayers.Body, rsiState);

        if (type == BorgStateType.Base)
            return;

        _spriteSystem.LayerSetVisible((uid, args.Sprite), BorgVisualLayers.LightStatus, false);
        _spriteSystem.LayerSetVisible((uid, args.Sprite), BorgVisualLayers.Light, false);
    }
}
