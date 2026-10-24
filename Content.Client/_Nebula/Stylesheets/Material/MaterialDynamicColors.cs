// SPDX-License-Identifier: MIT

// Ported from materialyoucolor: https://github.com/T-Dynamos/materialyoucolor-python

namespace Content.Client._Nebula.Stylesheets.Material;

/// <summary>
/// Material 3 color roles, as defined by materialyoucolor 2.0.10.
/// </summary>
public static class MaterialDynamicColors
{
    private static bool IsFidelity(DynamicScheme s)
    {
        return s.Variant is Variant.Fidelity or Variant.Content;
    }

    private static bool IsMonochrome(DynamicScheme s)
    {
        return s.Variant == Variant.Monochrome;
    }

    private static DynamicColor HighestSurface(DynamicScheme s)
    {
        return s.IsDark ? SurfaceBright : SurfaceDim;
    }

    private static double FindDesiredChromaByTone(double hue, double chroma, double tone, bool byDecreasingTone)
    {
        var answer = tone;
        var closestToChroma = Hct.From(hue, chroma, tone);

        if (closestToChroma.Chroma >= chroma)
            return answer;

        var chromaPeak = closestToChroma.Chroma;

        while (closestToChroma.Chroma < chroma)
        {
            answer += byDecreasingTone ? -1.0 : 1.0;
            var potentialSolution = Hct.From(hue, chroma, answer);

            if (chromaPeak > potentialSolution.Chroma)
                break;

            if (Math.Abs(potentialSolution.Chroma - chroma) < 0.4)
                break;

            var potentialDelta = Math.Abs(potentialSolution.Chroma - chroma);
            var currentDelta = Math.Abs(closestToChroma.Chroma - chroma);

            if (potentialDelta < currentDelta)
                closestToChroma = potentialSolution;

            chromaPeak = Math.Max(chromaPeak, potentialSolution.Chroma);
        }

        return answer;
    }

    private static double SecondaryContainerTone(DynamicScheme s)
    {
        var initialTone = s.IsDark ? 30 : 90;

        if (IsMonochrome(s))
            return s.IsDark ? 30 : 85;

        if (IsFidelity(s))
            return initialTone;

        return FindDesiredChromaByTone(s.SecondaryPalette.Hue, s.SecondaryPalette.Chroma, initialTone, !s.IsDark);
    }

    private static double OnSecondaryContainerTone(DynamicScheme s)
    {
        if (IsMonochrome(s))
            return s.IsDark ? 90 : 10;

        if (!IsFidelity(s))
            return s.IsDark ? 90 : 30;

        return DynamicColor.ForegroundTone(SecondaryContainer.Tone(s), 4.5);
    }

    /// <remarks>
    /// materialyoucolor checks IsMonochrome where upstream checks IsFidelity, kept for parity with end-4.
    /// </remarks>
    private static double TertiaryContainerTone(DynamicScheme s)
    {
        if (!IsMonochrome(s))
            return s.IsDark ? 60 : 49;

        if (!IsFidelity(s))
            return s.IsDark ? 30 : 90;

        return DislikeAnalyzer.FixIfDisliked(s.TertiaryPalette.GetHct(s.SourceColorHct.Tone)).Tone;
    }

    private static double OnTertiaryContainerTone(DynamicScheme s)
    {
        if (!IsMonochrome(s))
            return s.IsDark ? 0 : 100;

        if (!IsFidelity(s))
            return s.IsDark ? 90 : 30;

        return DynamicColor.ForegroundTone(TertiaryContainer.Tone(s), 4.5);
    }

    private static ToneDeltaPair PrimaryPair()
    {
        return new ToneDeltaPair(PrimaryContainer, Primary, 10, TonePolarity.Nearer, false);
    }

    private static ToneDeltaPair SecondaryPair()
    {
        return new ToneDeltaPair(SecondaryContainer, Secondary, 10, TonePolarity.Nearer, false);
    }

    private static ToneDeltaPair TertiaryPair()
    {
        return new ToneDeltaPair(TertiaryContainer, Tertiary, 10, TonePolarity.Nearer, false);
    }

    private static ToneDeltaPair ErrorPair()
    {
        return new ToneDeltaPair(ErrorContainer, Error, 10, TonePolarity.Nearer, false);
    }

    public static readonly DynamicColor PrimaryPaletteKeyColor = new(
        "primary_palette_key_color",
        s => s.PrimaryPalette,
        s => s.PrimaryPalette.KeyColor.Tone);

    public static readonly DynamicColor Background = new(
        "background",
        s => s.NeutralPalette,
        s => s.IsDark ? 6 : 98);

    public static readonly DynamicColor OnBackground = new(
        "on_background",
        s => s.NeutralPalette,
        s => s.IsDark ? 90 : 10,
        background: _ => Background,
        contrastCurve: new ContrastCurve(3, 3, 4.5, 7));

    public static readonly DynamicColor Surface = new(
        "surface",
        s => s.NeutralPalette,
        s => s.IsDark ? 6 : 98);

    public static readonly DynamicColor SurfaceDim = new(
        "surface_dim",
        s => s.NeutralPalette,
        s => s.IsDark ? 6 : new ContrastCurve(87, 87, 80, 75).Get(s.ContrastLevel));

    public static readonly DynamicColor SurfaceBright = new(
        "surface_bright",
        s => s.NeutralPalette,
        s => s.IsDark ? new ContrastCurve(24, 24, 29, 34).Get(s.ContrastLevel) : 98);

    public static readonly DynamicColor SurfaceContainerLowest = new(
        "surface_container_lowest",
        s => s.NeutralPalette,
        s => s.IsDark ? new ContrastCurve(4, 4, 2, 0).Get(s.ContrastLevel) : 100);

    public static readonly DynamicColor SurfaceContainerLow = new(
        "surface_container_low",
        s => s.NeutralPalette,
        s => s.IsDark
            ? new ContrastCurve(10, 10, 11, 12).Get(s.ContrastLevel)
            : new ContrastCurve(96, 96, 96, 95).Get(s.ContrastLevel));

    public static readonly DynamicColor SurfaceContainer = new(
        "surface_container",
        s => s.NeutralPalette,
        s => s.IsDark
            ? new ContrastCurve(12, 12, 16, 20).Get(s.ContrastLevel)
            : new ContrastCurve(94, 94, 92, 90).Get(s.ContrastLevel));

    public static readonly DynamicColor SurfaceContainerHigh = new(
        "surface_container_high",
        s => s.NeutralPalette,
        s => s.IsDark
            ? new ContrastCurve(17, 17, 21, 25).Get(s.ContrastLevel)
            : new ContrastCurve(92, 92, 88, 85).Get(s.ContrastLevel));

    public static readonly DynamicColor SurfaceContainerHighest = new(
        "surface_container_highest",
        s => s.NeutralPalette,
        s => s.IsDark
            ? new ContrastCurve(22, 22, 26, 30).Get(s.ContrastLevel)
            : new ContrastCurve(90, 90, 84, 80).Get(s.ContrastLevel));

    public static readonly DynamicColor OnSurface = new(
        "on_surface",
        s => s.NeutralPalette,
        s => s.IsDark ? 90 : 10,
        background: HighestSurface,
        contrastCurve: new ContrastCurve(4.5, 7, 11, 21));

    public static readonly DynamicColor SurfaceVariant = new(
        "surface_variant",
        s => s.NeutralVariantPalette,
        s => s.IsDark ? 30 : 90);

    public static readonly DynamicColor OnSurfaceVariant = new(
        "on_surface_variant",
        s => s.NeutralVariantPalette,
        s => s.IsDark ? 80 : 30,
        background: HighestSurface,
        contrastCurve: new ContrastCurve(3, 4.5, 7, 11));

    public static readonly DynamicColor InverseSurface = new(
        "inverse_surface",
        s => s.NeutralPalette,
        s => s.IsDark ? 90 : 20);

    public static readonly DynamicColor InverseOnSurface = new(
        "inverse_on_surface",
        s => s.NeutralPalette,
        s => s.IsDark ? 20 : 95,
        background: _ => InverseSurface,
        contrastCurve: new ContrastCurve(4.5, 7, 11, 21));

    public static readonly DynamicColor Outline = new(
        "outline",
        s => s.NeutralVariantPalette,
        s => s.IsDark ? 60 : 50,
        background: HighestSurface,
        contrastCurve: new ContrastCurve(1.5, 3, 4.5, 7));

    public static readonly DynamicColor OutlineVariant = new(
        "outline_variant",
        s => s.NeutralVariantPalette,
        s => s.IsDark ? 30 : 80,
        background: HighestSurface,
        contrastCurve: new ContrastCurve(1, 1, 3, 4.5));

    public static readonly DynamicColor Shadow = new(
        "shadow",
        s => s.NeutralPalette,
        _ => 0);

    public static readonly DynamicColor Scrim = new(
        "scrim",
        s => s.NeutralPalette,
        _ => 0);

    public static readonly DynamicColor SurfaceTint = new(
        "surface_tint",
        s => s.PrimaryPalette,
        s => s.IsDark ? 80 : 40);

    public static readonly DynamicColor Primary = new(
        "primary",
        s => s.PrimaryPalette,
        s => IsMonochrome(s) ? 100 : s.IsDark ? 80 : 40,
        background: HighestSurface,
        contrastCurve: new ContrastCurve(3, 4.5, 7, 7),
        toneDeltaPair: _ => PrimaryPair());

    public static readonly DynamicColor OnPrimary = new(
        "on_primary",
        s => s.PrimaryPalette,
        s => IsMonochrome(s) ? 10 : s.IsDark ? 20 : 100,
        background: _ => Primary,
        contrastCurve: new ContrastCurve(4.5, 7, 11, 21));

    public static readonly DynamicColor PrimaryContainer = new(
        "primary_container",
        s => s.PrimaryPalette,
        s => IsFidelity(s) ? s.SourceColorHct.Tone : IsMonochrome(s) ? 85 : s.IsDark ? 30 : 90,
        background: HighestSurface,
        contrastCurve: new ContrastCurve(1, 1, 3, 4.5),
        toneDeltaPair: _ => PrimaryPair());

    public static readonly DynamicColor OnPrimaryContainer = new(
        "on_primary_container",
        s => s.PrimaryPalette,
        s => IsFidelity(s)
            ? DynamicColor.ForegroundTone(PrimaryContainer.Tone(s), 4.5)
            : IsMonochrome(s) ? 0 : s.IsDark ? 90 : 30,
        background: _ => PrimaryContainer,
        contrastCurve: new ContrastCurve(3, 4.5, 7, 11));

    public static readonly DynamicColor InversePrimary = new(
        "inverse_primary",
        s => s.PrimaryPalette,
        s => s.IsDark ? 40 : 80,
        background: _ => InverseSurface,
        contrastCurve: new ContrastCurve(3, 4.5, 7, 7));

    public static readonly DynamicColor Secondary = new(
        "secondary",
        s => s.SecondaryPalette,
        s => s.IsDark ? 80 : 40,
        background: HighestSurface,
        contrastCurve: new ContrastCurve(3, 4.5, 7, 7),
        toneDeltaPair: _ => SecondaryPair());

    public static readonly DynamicColor OnSecondary = new(
        "on_secondary",
        s => s.SecondaryPalette,
        s => IsMonochrome(s) ? 10 : s.IsDark ? 20 : 100,
        background: _ => Secondary,
        contrastCurve: new ContrastCurve(4.5, 7, 11, 21));

    public static readonly DynamicColor SecondaryContainer = new(
        "secondary_container",
        s => s.SecondaryPalette,
        SecondaryContainerTone,
        background: HighestSurface,
        contrastCurve: new ContrastCurve(1, 1, 3, 4.5),
        toneDeltaPair: _ => SecondaryPair());

    public static readonly DynamicColor OnSecondaryContainer = new(
        "on_secondary_container",
        s => s.SecondaryPalette,
        OnSecondaryContainerTone,
        background: _ => SecondaryContainer,
        contrastCurve: new ContrastCurve(3, 4.5, 7, 11));

    public static readonly DynamicColor Tertiary = new(
        "tertiary",
        s => s.TertiaryPalette,
        s => IsMonochrome(s) ? s.IsDark ? 90 : 25 : s.IsDark ? 80 : 40,
        background: HighestSurface,
        contrastCurve: new ContrastCurve(3, 4.5, 7, 7),
        toneDeltaPair: _ => TertiaryPair());

    public static readonly DynamicColor OnTertiary = new(
        "on_tertiary",
        s => s.TertiaryPalette,
        s => IsMonochrome(s) ? s.IsDark ? 10 : 90 : s.IsDark ? 20 : 100,
        background: _ => Tertiary,
        contrastCurve: new ContrastCurve(4.5, 7, 11, 21));

    public static readonly DynamicColor TertiaryContainer = new(
        "tertiary_container",
        s => s.TertiaryPalette,
        TertiaryContainerTone,
        background: HighestSurface,
        contrastCurve: new ContrastCurve(1, 1, 3, 4.5),
        toneDeltaPair: _ => TertiaryPair());

    public static readonly DynamicColor OnTertiaryContainer = new(
        "on_tertiary_container",
        s => s.TertiaryPalette,
        OnTertiaryContainerTone,
        background: _ => TertiaryContainer,
        contrastCurve: new ContrastCurve(3, 4.5, 7, 11));

    public static readonly DynamicColor Error = new(
        "error",
        s => s.ErrorPalette,
        s => s.IsDark ? 80 : 40,
        background: HighestSurface,
        contrastCurve: new ContrastCurve(3, 4.5, 7, 7),
        toneDeltaPair: _ => ErrorPair());

    public static readonly DynamicColor OnError = new(
        "on_error",
        s => s.ErrorPalette,
        s => s.IsDark ? 20 : 100,
        background: _ => Error,
        contrastCurve: new ContrastCurve(4.5, 7, 11, 21));

    public static readonly DynamicColor ErrorContainer = new(
        "error_container",
        s => s.ErrorPalette,
        s => s.IsDark ? 30 : 90,
        background: HighestSurface,
        contrastCurve: new ContrastCurve(1, 1, 3, 4.5),
        toneDeltaPair: _ => ErrorPair());

    public static readonly DynamicColor OnErrorContainer = new(
        "on_error_container",
        s => s.ErrorPalette,
        s => s.IsDark ? 90 : IsMonochrome(s) ? 10 : 30,
        background: _ => ErrorContainer,
        contrastCurve: new ContrastCurve(3, 4.5, 7, 11));

    /// <summary>
    /// Every role in the order the Python script prints them, used by the parity tests.
    /// </summary>
    public static readonly DynamicColor[] All =
    [
        PrimaryPaletteKeyColor,
        Background, OnBackground,
        Surface, SurfaceDim, SurfaceBright,
        SurfaceContainerLowest, SurfaceContainerLow, SurfaceContainer, SurfaceContainerHigh, SurfaceContainerHighest,
        OnSurface, SurfaceVariant, OnSurfaceVariant, InverseSurface, InverseOnSurface,
        Outline, OutlineVariant, Shadow, Scrim, SurfaceTint,
        Primary, OnPrimary, PrimaryContainer, OnPrimaryContainer, InversePrimary,
        Secondary, OnSecondary, SecondaryContainer, OnSecondaryContainer,
        Tertiary, OnTertiary, TertiaryContainer, OnTertiaryContainer,
        Error, OnError, ErrorContainer, OnErrorContainer,
    ];
}
