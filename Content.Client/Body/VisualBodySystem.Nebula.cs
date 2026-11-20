// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Humanoid.Markings;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Body;

/// <summary>
/// Nebula - WolfGate. Draws the size step and skin-toned art of markings with <see cref="MarkingPrototype.SizedStates"/>.
/// </summary>
public sealed partial class VisualBodySystem
{
    private void ApplySizedState(Entity<VisualOrganMarkingsComponent> organ,
        Entity<SpriteComponent> target,
        int spriteLayer,
        MarkingPrototype proto,
        Marking marking,
        int spriteIndex)
    {
        if (proto.SizedStates is not { } sized
            || !_sprite.TryGetLayer(target.AsNullable(), spriteLayer, out var layer, false)
            || layer.ActualRsi is not { } rsi)
            return;

        var tone = ProtoMan.Resolve(organ.Comp.MarkingData.Group, out var group) ? group.ArtTone : string.Empty;

        // toned art first, then the neutral art for shapes that have no toned variant
        if (TrySetState(target, spriteLayer, rsi, sized.State(spriteIndex, marking.Size, tone))
            || tone.Length > 0 && TrySetState(target, spriteLayer, rsi, sized.State(spriteIndex, marking.Size, string.Empty)))
            return;

        // no art for this step, e.g. small breasts have nothing behind the body
        _sprite.LayerSetRsiState(target.AsNullable(), spriteLayer, RSI.StateId.Invalid);
    }

    private bool TrySetState(Entity<SpriteComponent> target, int spriteLayer, RSI rsi, string? state)
    {
        if (state == null || !rsi.TryGetState(state, out _))
            return false;

        _sprite.LayerSetRsiState(target.AsNullable(), spriteLayer, state);
        return true;
    }
}
