// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/02837fb57c50d53cddaeab30fe0e12477e02efda/Content.Client/_Orion/Sprite/EdgeConnections/EdgeConnectionVisualizerSystem.cs

using Content.Shared._Orion.Sprite.EdgeConnections;
using Robust.Client.GameObjects;

namespace Content.Client._Orion.Sprite.EdgeConnections;

public sealed class EdgeConnectionVisualizerSystem : VisualizerSystem<EdgeConnectionComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, EdgeConnectionComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var sprite = (uid, args.Sprite);
        if (!SpriteSystem.TryGetLayer(sprite, component.Layer, out var layer, false))
            return;

        component.Supported ??= layer.ActualRsi?.Path == SpriteSystem.TextureRoot / component.BaseSprite
                                && layer.State == component.BaseState;

        if (component.Supported != true)
            return;

        var rsi = component.BaseSprite;
        var state = component.BaseState;
        if (component.Sprite is { } connectedSprite
            && AppearanceSystem.TryGetData<EdgeConnectionDirections>(uid, EdgeConnectionVisuals.ConnectionMask, out var mask, args.Component)
            && component.States.TryGetValue(mask, out var connectedState))
        {
            rsi = connectedSprite;
            state = connectedState;
        }

        if (layer.State == state && layer.ActualRsi?.Path == SpriteSystem.TextureRoot / rsi)
            return;

        SpriteSystem.LayerSetRsi(layer, rsi, state);
    }
}
