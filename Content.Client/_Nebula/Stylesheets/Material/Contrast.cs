// SPDX-License-Identifier: MIT

// Ported from materialyoucolor: https://github.com/T-Dynamos/materialyoucolor-python

using static Content.Client._Nebula.Stylesheets.Material.MaterialMath;

namespace Content.Client._Nebula.Stylesheets.Material;

/// <summary>
/// WCAG contrast ratios between tones.
/// </summary>
public static class Contrast
{
    public static double RatioOfTones(double toneA, double toneB)
    {
        toneA = Clamp(0.0, 100.0, toneA);
        toneB = Clamp(0.0, 100.0, toneB);
        return RatioOfYs(YFromLstar(toneA), YFromLstar(toneB));
    }

    private static double RatioOfYs(double y1, double y2)
    {
        var lighter = y1 > y2 ? y1 : y2;
        var darker = lighter == y2 ? y1 : y2;
        return (lighter + 5.0) / (darker + 5.0);
    }

    public static double Lighter(double tone, double ratio)
    {
        if (tone is < 0.0 or > 100.0)
            return -1.0;

        var darkY = YFromLstar(tone);
        var lightY = ratio * (darkY + 5.0) - 5.0;
        var realContrast = RatioOfYs(lightY, darkY);
        var delta = Math.Abs(realContrast - ratio);

        if (realContrast < ratio && delta > 0.04)
            return -1.0;

        var value = LstarFromY(lightY) + 0.4;
        return value is < 0 or > 100 ? -1.0 : value;
    }

    public static double Darker(double tone, double ratio)
    {
        if (tone is < 0.0 or > 100.0)
            return -1.0;

        var lightY = YFromLstar(tone);
        var darkY = (lightY + 5.0) / ratio - 5.0;
        var realContrast = RatioOfYs(lightY, darkY);
        var delta = Math.Abs(realContrast - ratio);

        if (realContrast < ratio && delta > 0.04)
            return -1.0;

        var value = LstarFromY(darkY) - 0.4;
        return value is < 0 or > 100 ? -1.0 : value;
    }

    public static double LighterUnsafe(double tone, double ratio)
    {
        var safe = Lighter(tone, ratio);
        return safe < 0.0 ? 100.0 : safe;
    }

    public static double DarkerUnsafe(double tone, double ratio)
    {
        var safe = Darker(tone, ratio);
        return safe < 0.0 ? 0.0 : safe;
    }
}

/// <summary>
/// Contrast ratio to aim for at each contrast level.
/// </summary>
public sealed class ContrastCurve(double low, double normal, double medium, double high)
{
    public double Get(double contrastLevel)
    {
        if (contrastLevel <= -1.0)
            return low;

        if (contrastLevel < 0.0)
            return Lerp(low, normal, contrastLevel + 1);

        if (contrastLevel < 0.5)
            return Lerp(normal, medium, contrastLevel / 0.5);

        if (contrastLevel < 1.0)
            return Lerp(medium, high, (contrastLevel - 0.5) / 0.5);

        return high;
    }
}

/// <summary>
/// Keeps hues out of the yellow-green range that people tend to dislike.
/// </summary>
public static class DislikeAnalyzer
{
    public static bool IsDisliked(Hct hct)
    {
        var huePasses = RoundToInt(hct.Hue) is >= 90 and <= 111;
        var chromaPasses = RoundToInt(hct.Chroma) > 16;
        var tonePasses = RoundToInt(hct.Tone) < 65;
        return huePasses && chromaPasses && tonePasses;
    }

    public static Hct FixIfDisliked(Hct hct)
    {
        return IsDisliked(hct) ? Hct.From(hct.Hue, hct.Chroma, 70.0) : hct;
    }
}
