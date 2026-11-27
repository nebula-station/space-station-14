// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Client/PDA/PdaSkinSystem.cs

using Content.Client.PDA;
using Content.Client._Arcane.Clothing;
using Content.Shared._Arcane.PDA;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._Arcane.PDA;

/// <summary>
/// Applies <see cref="PdaSkinPrototype"/> to PDA sprites and shows the screen state.
/// </summary>
public sealed partial class PdaSkinSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private const string FlashlightState = "light_overlay";
    private const string IdLightState = "id_overlay";

    [SubscribeLocalEvent]
    private void OnStartup(Entity<PdaScreenComponent> ent, ref ComponentStartup args)
    {
        if (MetaData(ent).EntityPrototype is { } proto)
            ApplySkin(ent, proto);
    }

    /// <summary>
    /// A disguise copies its whole sprite over this one, so the skin of the disguise goes on top again.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnChameleonSpriteUpdated(Entity<PdaScreenComponent> ent, ref ChameleonSpriteUpdatedEvent args)
    {
        ApplySkin(ent, args.Disguise);
    }

    private void ApplySkin(Entity<PdaScreenComponent> ent, EntityPrototype proto)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite) || GetSkin(proto) is not { } skin)
            return;

        var spriteEnt = (ent.Owner, sprite);

        // Already skinned: the disguise had no sprite to copy over the layers.
        if (_sprite.LayerMapTryGet(spriteEnt, PdaScreenLayers.Screen, out _, false))
            return;

        if (!_sprite.LayerMapTryGet(spriteEnt, PdaVisualizerSystem.PdaVisualLayers.Base, out var baseIndex, false))
            return;

        // The PDA visualizer keeps setting the PDA type state on the base layer, so it stays on its own RSI and gets hidden.
        _sprite.LayerSetVisible(spriteEnt, baseIndex, false);

        var index = baseIndex + 1;
        _sprite.AddRsiLayer(spriteEnt, skin.BaseState, skin.Sprite, index);
        _sprite.LayerSetColor(spriteEnt, index, skin.BaseColor ?? Color.White);
        index++;

        foreach (var layer in skin.Layers)
        {
            _sprite.AddRsiLayer(spriteEnt, layer.State, skin.Sprite, index);
            _sprite.LayerSetColor(spriteEnt, index, layer.Color ?? Color.White);
            index++;
        }

        _sprite.AddRsiLayer(spriteEnt, ent.Comp.OffState, skin.Sprite, index);
        _sprite.LayerMapSet(spriteEnt, PdaScreenLayers.Screen, index);

        // Wide PDAs use other overlay states; the skin RSI only has the regular ones.
        if (_sprite.LayerMapTryGet(spriteEnt, PdaVisualizerSystem.PdaVisualLayers.Flashlight, out var flashlight, false))
            _sprite.LayerSetRsi(spriteEnt, flashlight, skin.Sprite, FlashlightState);
        if (_sprite.LayerMapTryGet(spriteEnt, PdaVisualizerSystem.PdaVisualLayers.IdLight, out var idLight, false))
            _sprite.LayerSetRsi(spriteEnt, idLight, skin.Sprite, IdLightState);

        if (TryComp<PdaBorderColorComponent>(ent, out var border))
        {
            if (skin.BorderColor is { } borderColor)
                border.BorderColor = borderColor.ToHex();
            if (skin.AccentColor is { } accentColor)
                border.AccentVColor = accentColor.ToHex();
        }

        if (TryComp<AppearanceComponent>(ent, out var appearance))
            UpdateScreen(ent, sprite, appearance);
    }

    [SubscribeLocalEvent(after: [typeof(PdaVisualizerSystem)])]
    private void OnAppearanceChange(Entity<PdaScreenComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        UpdateScreen(ent, sprite, args.Component);
    }

    private void UpdateScreen(Entity<PdaScreenComponent> ent, SpriteComponent sprite, AppearanceComponent appearance)
    {
        if (!_appearance.TryGetData<string>(ent, PdaScreenVisuals.State, out var state, appearance)
            || !_sprite.LayerMapTryGet((ent, sprite), PdaScreenLayers.Screen, out var screen, false))
            return;

        _sprite.LayerSetRsiState((ent, sprite), screen, state);
    }

    /// <summary>
    /// The skin for this PDA's prototype or its closest parent.
    /// </summary>
    private PdaSkinPrototype? GetSkin(EntityPrototype proto)
    {
        if (_proto.TryIndex<PdaSkinPrototype>(proto.ID, out var skin))
            return skin;

        foreach (var parent in _proto.EnumerateAllParents<EntityPrototype>(proto.ID))
        {
            if (_proto.TryIndex(parent.id, out skin))
                return skin;
        }

        return null;
    }
}
