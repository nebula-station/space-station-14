// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/accb2613b4ea35df58380cc8c7c82cfb2968670a/Content.Arcane.Client/Economy/CreditHolochipVisualizerSystem.cs

using Content.Shared._Arcane.Economy;
using Robust.Client.GameObjects;

namespace Content.Client._Arcane.Economy;

public sealed partial class CreditHolochipVisualizerSystem : VisualizerSystem<CreditHolochipComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, CreditHolochipComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        if (AppearanceSystem.TryGetData<string>(uid, CreditHolochipVisuals.BaseState, out var baseState, args.Component))
            SpriteSystem.LayerSetRsiState((uid, sprite), "base", baseState);

        if (AppearanceSystem.TryGetData<string>(uid, CreditHolochipVisuals.OverlayState, out var overlayState, args.Component))
            SpriteSystem.LayerSetRsiState((uid, sprite), "overlay", overlayState);

        if (AppearanceSystem.TryGetData<Color>(uid, CreditHolochipVisuals.BaseColor, out var color, args.Component))
            SpriteSystem.LayerSetColor((uid, sprite), "base", color);
    }
}
