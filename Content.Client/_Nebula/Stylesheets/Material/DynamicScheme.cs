// SPDX-License-Identifier: MIT

// Ported from materialyoucolor: https://github.com/T-Dynamos/materialyoucolor-python

namespace Content.Client._Nebula.Stylesheets.Material;

public enum Variant : byte
{
    Monochrome,
    Neutral,
    TonalSpot,
    Vibrant,
    Expressive,
    Fidelity,
    Content,
    Rainbow,
    FruitSalad,
}

/// <summary>
/// Source color, mode and the tonal palettes that <see cref="MaterialDynamicColors"/> pick their tones from.
/// </summary>
public sealed class DynamicScheme
{
    public Hct SourceColorHct { get; }
    public Variant Variant { get; }
    public bool IsDark { get; }
    public double ContrastLevel { get; }

    public TonalPalette PrimaryPalette { get; }
    public TonalPalette SecondaryPalette { get; }
    public TonalPalette TertiaryPalette { get; }
    public TonalPalette NeutralPalette { get; }
    public TonalPalette NeutralVariantPalette { get; }
    public TonalPalette ErrorPalette { get; } = TonalPalette.FromHueAndChroma(25.0, 84.0);

    private DynamicScheme(
        Hct source,
        Variant variant,
        bool isDark,
        double contrastLevel,
        TonalPalette primary,
        TonalPalette secondary,
        TonalPalette tertiary,
        TonalPalette neutral,
        TonalPalette neutralVariant)
    {
        SourceColorHct = source;
        Variant = variant;
        IsDark = isDark;
        ContrastLevel = contrastLevel;
        PrimaryPalette = primary;
        SecondaryPalette = secondary;
        TertiaryPalette = tertiary;
        NeutralPalette = neutral;
        NeutralVariantPalette = neutralVariant;
    }

    /// <summary>
    /// The "scheme-content" variant: palettes follow the source color's own chroma.
    /// </summary>
    public static DynamicScheme Content(Hct source, bool isDark, double contrastLevel = 0.0)
    {
        var tertiary = DislikeAnalyzer.FixIfDisliked(new TemperatureCache(source).Analogous(3, 6)[2]);

        return new DynamicScheme(
            source,
            Variant.Content,
            isDark,
            contrastLevel,
            TonalPalette.FromHueAndChroma(source.Hue, source.Chroma),
            TonalPalette.FromHueAndChroma(source.Hue, Math.Max(source.Chroma - 32.0, source.Chroma * 0.5)),
            TonalPalette.FromHct(Hct.FromInt(tertiary.Argb)),
            TonalPalette.FromHueAndChroma(source.Hue, source.Chroma / 8.0),
            TonalPalette.FromHueAndChroma(source.Hue, source.Chroma / 8.0 + 4.0));
    }
}
