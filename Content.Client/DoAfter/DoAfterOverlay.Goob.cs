// SPDX-License-Identifier: AGPL-3.0-or-later

// Ported from https://github.com/Goob-Station/Goob-Station/blob/707e36693d1d9289a4f04921c5e070b1e4195589/Content.Client/DoAfter/DoAfterOverlay.Goob.cs

namespace Content.Client.DoAfter;

public sealed partial class DoAfterOverlay
{
    private static Color ApplyOverrideAlpha(Color? colorOverride, Color color)
    {
        return colorOverride is { } overrideColor
            ? overrideColor.WithAlpha(overrideColor.A * color.A)
            : color;
    }
}
