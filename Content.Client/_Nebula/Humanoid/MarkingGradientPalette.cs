// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client.Stylesheets.Colorspace;

namespace Content.Client._Nebula.Humanoid;

/// <summary>
/// What a suggested gradient color should be relative to the first color of the marking.
/// </summary>
public enum MarkingGradientSuggestion : byte
{
    Lighter,
    Analogous,
    Contrast,
}

/// <summary>
/// Suggests a second gradient color in OkLCh, the polar form of the space the gradient shader blends in,
/// so steps of hue look as even in the result as they do here.
/// </summary>
public static class MarkingGradientPalette
{
    /// <summary>
    /// Colors below this chroma have no hue worth keeping, so the tables of neutral hues apply to them.
    /// </summary>
    private const float NeutralChroma = 0.035f;

    private const float MaxLightness = 0.96f;
    private const float WarmHue = 65f;
    private const int GamutSteps = 24;

    private static readonly float[] LighterSteps = [0.3f, 0.5f, 0.7f];
    private static readonly float[] AnalogousShifts = [35f, -35f, 70f, -70f];
    private static readonly float[] AnalogousNeutralHues = [260f, 320f, 190f, 40f];
    private static readonly float[] ContrastShifts = [180f, 140f, -140f, 110f];
    private static readonly float[] ContrastNeutralHues = [230f, 350f, 140f, 60f, 290f];

    /// <param name="roots">The color to start from, in sRGB. Its alpha is ignored.</param>
    /// <param name="kind">The relation to <paramref name="roots"/>.</param>
    /// <param name="variant">Non-negative number of the variant, wraps around the list of variants.</param>
    /// <returns>An opaque sRGB color.</returns>
    public static Color Suggest(Color roots, MarkingGradientSuggestion kind, int variant)
    {
        var lch = Color.ToLch(roots.WithAlpha(1f).LabFromSrgb());

        return kind switch
        {
            MarkingGradientSuggestion.Analogous => Analogous(lch, variant),
            MarkingGradientSuggestion.Contrast => Contrast(lch, variant),
            _ => Lighter(lch, variant),
        };
    }

    private static Color Lighter(Vector4 lch, int variant)
    {
        var step = LighterSteps[variant % LighterSteps.Length];
        var lightness = lch.X + (MaxLightness - lch.X) * step;

        return lch.Y < NeutralChroma
            ? FromLch(lightness, 0.04f + step * 0.05f, WarmHue * MathF.PI / 180f)
            : FromLch(lightness, lch.Y * 0.8f, lch.Z);
    }

    private static Color Analogous(Vector4 lch, int variant)
    {
        var lightness = Math.Clamp(lch.X, 0.45f, 0.85f);

        return lch.Y < NeutralChroma
            ? FromLch(lightness, 0.09f, AnalogousNeutralHues[variant % AnalogousNeutralHues.Length] * MathF.PI / 180f)
            : FromLch(lightness, MathF.Max(lch.Y, 0.07f), lch.Z + AnalogousShifts[variant % AnalogousShifts.Length] * MathF.PI / 180f);
    }

    /// <summary>
    /// Moves lightness along with the hue, because a dark accent disappears on dark roots whatever its hue is.
    /// </summary>
    private static Color Contrast(Vector4 lch, int variant)
    {
        var lightness = lch.X < 0.55f
            ? MathF.Max(lch.X + 0.35f, 0.7f)
            : MathF.Min(lch.X - 0.35f, 0.45f);

        return lch.Y < NeutralChroma
            ? FromLch(lightness, 0.15f, ContrastNeutralHues[variant % ContrastNeutralHues.Length] * MathF.PI / 180f)
            : FromLch(lightness, MathF.Max(lch.Y, 0.13f), lch.Z + ContrastShifts[variant % ContrastShifts.Length] * MathF.PI / 180f);
    }

    /// <summary>
    /// Lowers chroma step by step until the color fits sRGB, which keeps its hue and lightness unlike clamping channels.
    /// </summary>
    private static Color FromLch(float lightness, float chroma, float hue)
    {
        for (var step = 0; step < GamutSteps; step++)
        {
            var lab = Color.FromLch(new Vector4(lightness, chroma, hue, 1f));
            if (InGamut(Color.FromLab(lab)))
                return lab.LabToSrgb();

            chroma *= 0.9f;
        }

        return Color.FromLch(new Vector4(lightness, chroma, hue, 1f)).LabToSrgb();
    }

    private static bool InGamut(Color linear)
    {
        return linear.R is >= 0f and <= 1f
            && linear.G is >= 0f and <= 1f
            && linear.B is >= 0f and <= 1f;
    }
}
