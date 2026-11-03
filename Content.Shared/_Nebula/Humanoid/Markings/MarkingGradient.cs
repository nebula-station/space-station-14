// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.Humanoid.Markings;

/// <summary>
/// How the second color of a <see cref="MarkingGradient"/> is laid over the sprite.
/// The order must match the <c>gradientStyle</c> branches in <c>marking_gradient.swsl</c>.
/// </summary>
[Serializable, NetSerializable]
public enum MarkingGradientStyle : byte
{
    Ombre,
    Split,
    Underdye,
    Streaks,
    DipDye,
    FrontStrands,
}

/// <summary>
/// Blends a marking from its own color into <see cref="Color"/>.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial record struct MarkingGradient
{
    [DataField]
    public MarkingGradientStyle Style;

    /// <summary>
    /// The second color. The first one is the color of the marking's sprite.
    /// </summary>
    [DataField]
    public Color Color = Color.White;

    /// <summary>
    /// Style-specific share of the accent from 0 to 1, such as the height of the transition or the density of strands.
    /// </summary>
    [DataField]
    public float Offset = 0.5f;

    public MarkingGradient()
    {
    }

    /// <summary>
    /// Returns a copy that is safe to render, since the values can come straight from a client.
    /// </summary>
    public MarkingGradient Sanitized()
    {
        return this with
        {
            Style = Enum.IsDefined(typeof(MarkingGradientStyle), Style) ? Style : MarkingGradientStyle.Ombre,
            Color = HumanoidCharacterAppearance.ClampColor(Color),
            Offset = float.IsFinite(Offset) ? Math.Clamp(Offset, 0f, 1f) : 0.5f,
        };
    }
}
