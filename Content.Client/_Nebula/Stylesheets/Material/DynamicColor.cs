// SPDX-License-Identifier: MIT

// Ported from materialyoucolor: https://github.com/T-Dynamos/materialyoucolor-python

using static Content.Client._Nebula.Stylesheets.Material.MaterialMath;

namespace Content.Client._Nebula.Stylesheets.Material;

public enum TonePolarity : byte
{
    Darker,
    Lighter,
    Nearer,
    Farther,
}

/// <summary>
/// Two roles that must keep <see cref="Delta"/> tones between each other.
/// </summary>
public sealed record ToneDeltaPair(
    DynamicColor RoleA,
    DynamicColor RoleB,
    double Delta,
    TonePolarity Polarity,
    bool StayTogether);

/// <summary>
/// A scheme role: picks a tone from a palette, then adjusts it for contrast against its background.
/// </summary>
/// <remarks>
/// Ported as is, including the places where materialyoucolor deviates from upstream Material Color Utilities,
/// because end-4 generates its colors with it.
/// </remarks>
public sealed class DynamicColor
{
    public string Name { get; }
    public Func<DynamicScheme, TonalPalette> Palette { get; }
    public Func<DynamicScheme, double> Tone { get; }
    public Func<DynamicScheme, DynamicColor>? Background { get; }
    public Func<DynamicScheme, DynamicColor>? SecondBackground { get; }
    public ContrastCurve? ContrastCurve { get; }
    public Func<DynamicScheme, ToneDeltaPair>? ToneDeltaPair { get; }

    public DynamicColor(
        string name,
        Func<DynamicScheme, TonalPalette> palette,
        Func<DynamicScheme, double> tone,
        Func<DynamicScheme, DynamicColor>? background = null,
        Func<DynamicScheme, DynamicColor>? secondBackground = null,
        ContrastCurve? contrastCurve = null,
        Func<DynamicScheme, ToneDeltaPair>? toneDeltaPair = null)
    {
        Name = name;
        Palette = palette;
        Tone = tone;
        Background = background;
        SecondBackground = secondBackground;
        ContrastCurve = contrastCurve;
        ToneDeltaPair = toneDeltaPair;
    }

    public int GetArgb(DynamicScheme scheme)
    {
        return GetHct(scheme).Argb;
    }

    public Hct GetHct(DynamicScheme scheme)
    {
        return Palette(scheme).GetHct(GetTone(scheme));
    }

    /// <remarks>
    /// materialyoucolor defaults is_background to the truthy <c>bool</c> type, so every role gets the 50-60 tone nudge.
    /// </remarks>
    public double GetTone(DynamicScheme scheme)
    {
        var decreasingContrast = scheme.ContrastLevel < 0;

        if (ToneDeltaPair != null)
            return GetPairTone(scheme, ToneDeltaPair(scheme), decreasingContrast);

        var answer = Tone(scheme);

        if (Background == null || ContrastCurve == null)
            return answer;

        var bgTone = Background(scheme).GetTone(scheme);
        var desiredRatio = ContrastCurve.Get(scheme.ContrastLevel);

        if (Contrast.RatioOfTones(bgTone, answer) < desiredRatio)
            answer = ForegroundTone(bgTone, desiredRatio);

        if (decreasingContrast)
            answer = ForegroundTone(bgTone, desiredRatio);

        if (answer is >= 50 and < 60)
            answer = Contrast.RatioOfTones(49, bgTone) >= desiredRatio ? 49 : 60;

        if (SecondBackground == null)
            return answer;

        var bgTone1 = Background(scheme).GetTone(scheme);
        var bgTone2 = SecondBackground(scheme).GetTone(scheme);
        var upper = Math.Max(bgTone1, bgTone2);
        var lower = Math.Min(bgTone1, bgTone2);

        if (Contrast.RatioOfTones(upper, answer) >= desiredRatio && Contrast.RatioOfTones(lower, answer) >= desiredRatio)
            return answer;

        var lightOption = Contrast.Lighter(upper, desiredRatio);
        var darkOption = Contrast.Darker(lower, desiredRatio);
        var prefersLight = TonePrefersLightForeground(bgTone1) || TonePrefersLightForeground(bgTone2);

        return prefersLight && (lightOption == -1 || darkOption == -1) ? lightOption : darkOption;
    }

    /// <remarks>
    /// materialyoucolor moves the farther tone away from itself instead of from the nearer one. Python assigns both
    /// tones at once, so the farther tone is based on the nearer one before the 50-60 nudge.
    /// </remarks>
    private double GetPairTone(DynamicScheme scheme, ToneDeltaPair pair, bool decreasingContrast)
    {
        var bgTone = Background!(scheme).GetTone(scheme);

        var aIsNearer = pair.Polarity == TonePolarity.Nearer
                        || pair.Polarity == TonePolarity.Lighter && !scheme.IsDark
                        || pair.Polarity == TonePolarity.Darker && scheme.IsDark;

        var nearer = aIsNearer ? pair.RoleA : pair.RoleB;
        var farther = aIsNearer ? pair.RoleB : pair.RoleA;
        var amNearer = Name == nearer.Name;
        var expansionDir = scheme.IsDark ? 1 : -1;
        var delta = pair.Delta;

        var nContrast = nearer.ContrastCurve!.Get(scheme.ContrastLevel);
        var fContrast = farther.ContrastCurve!.Get(scheme.ContrastLevel);

        var nInitialTone = nearer.Tone(scheme);
        var nTone = Contrast.RatioOfTones(bgTone, nInitialTone) >= nContrast
            ? nInitialTone
            : ForegroundTone(bgTone, nContrast);

        var fInitialTone = farther.Tone(scheme);
        var fTone = Contrast.RatioOfTones(bgTone, fInitialTone) >= fContrast
            ? fInitialTone
            : ForegroundTone(bgTone, fContrast);

        if (decreasingContrast)
        {
            nTone = ForegroundTone(bgTone, nContrast);
            fTone = ForegroundTone(bgTone, fContrast);
        }

        if ((fTone - nTone) * expansionDir < delta)
            fTone = Math.Min(Math.Max(fTone - delta * expansionDir, 0), 100);

        if (nTone is >= 50 and < 60)
        {
            if (expansionDir > 0)
            {
                fTone = Math.Max(fTone, nTone + delta * expansionDir);
                nTone = 60;
            }
            else
            {
                fTone = Math.Min(fTone, nTone + delta * expansionDir);
                nTone = 49;
            }
        }
        else if (fTone is >= 50 and < 60)
        {
            if (pair.StayTogether)
            {
                if (expansionDir > 0)
                {
                    fTone = Math.Max(fTone, nTone + delta * expansionDir);
                    nTone = 60;
                }
                else
                {
                    fTone = Math.Min(fTone, nTone + delta * expansionDir);
                    nTone = 49;
                }
            }
            else
            {
                fTone = expansionDir > 0 ? 60 : 49;
            }
        }

        return amNearer ? nTone : fTone;
    }

    public static double ForegroundTone(double bgTone, double ratio)
    {
        var lighterTone = Contrast.LighterUnsafe(bgTone, ratio);
        var darkerTone = Contrast.DarkerUnsafe(bgTone, ratio);
        var lighterRatio = Contrast.RatioOfTones(lighterTone, bgTone);
        var darkerRatio = Contrast.RatioOfTones(darkerTone, bgTone);

        if (TonePrefersLightForeground(bgTone))
        {
            var negligibleDifference = Math.Abs(lighterRatio - darkerRatio) < 0.1
                                       && lighterRatio < ratio
                                       && darkerRatio < ratio;

            return lighterRatio >= ratio || lighterRatio >= darkerRatio || negligibleDifference
                ? lighterTone
                : darkerTone;
        }

        return darkerRatio >= ratio || darkerRatio >= lighterRatio ? darkerTone : lighterTone;
    }

    public static bool TonePrefersLightForeground(double tone)
    {
        return RoundToInt(tone) < 60;
    }
}
