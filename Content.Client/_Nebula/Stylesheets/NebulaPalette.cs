// SPDX-License-Identifier: GPL-3.0-only

// Ported from end-4 dots-hyprland: https://github.com/end-4/dots-hyprland

using Content.Client._Nebula.Stylesheets.Material;
using M = Content.Client._Nebula.Stylesheets.Material.MaterialDynamicColors;

namespace Content.Client._Nebula.Stylesheets;

/// <summary>
/// Material 3 "content" scheme generated from <see cref="Seed"/>, plus the layer colors end-4's dots-hyprland derives
/// from it in <c>Appearance.qml</c>.
/// </summary>
public sealed class NebulaPalette
{
    /// <summary>
    /// end-4's default content transparency, how much of the layer below shows through a nested layer.
    /// </summary>
    public const float ContentTransparency = 0.57f;

    /// <summary>
    /// end-4's terminal color harmonization, used for colors that don't come from the scheme.
    /// </summary>
    private const double HarmonyAmount = 0.6;
    private const double HarmonyThreshold = 100;
    private const double HarmonyToneBoost = 0.35;

    /// <summary>
    /// Material custom color blending: hue moves halfway to the primary, at most this many degrees.
    /// </summary>
    private const double CustomColorMaxRotation = 15;
    private const double CustomColorMinChroma = 48;

    /// <summary>
    /// Source of the <see cref="Info"/> colors, blue like Central Command.
    /// </summary>
    private static readonly Color InfoSource = Color.FromHex("#3A7BD5");

    public Color Seed { get; }
    public bool IsDark { get; }

    public Color PrimaryKey { get; }

    public Color Primary { get; }
    public Color OnPrimary { get; }
    public Color PrimaryContainer { get; }
    public Color OnPrimaryContainer { get; }
    public Color InversePrimary { get; }

    public Color Secondary { get; }
    public Color OnSecondary { get; }
    public Color SecondaryContainer { get; }
    public Color OnSecondaryContainer { get; }

    public Color Tertiary { get; }
    public Color OnTertiary { get; }
    public Color TertiaryContainer { get; }
    public Color OnTertiaryContainer { get; }

    public Color Error { get; }
    public Color OnError { get; }
    public Color ErrorContainer { get; }
    public Color OnErrorContainer { get; }

    public Color Success { get; }
    public Color OnSuccess { get; }
    public Color SuccessContainer { get; }
    public Color OnSuccessContainer { get; }

    /// <summary>
    /// Blue accent for Central Command things, a Material custom color blended with the scheme.
    /// </summary>
    public Color Info { get; }
    public Color OnInfo { get; }
    public Color InfoContainer { get; }
    public Color OnInfoContainer { get; }

    public Color Background { get; }
    public Color OnBackground { get; }
    public Color Surface { get; }
    public Color SurfaceDim { get; }
    public Color SurfaceBright { get; }
    public Color SurfaceContainerLowest { get; }
    public Color SurfaceContainerLow { get; }
    public Color SurfaceContainer { get; }
    public Color SurfaceContainerHigh { get; }
    public Color SurfaceContainerHighest { get; }
    public Color OnSurface { get; }
    public Color SurfaceVariant { get; }
    public Color OnSurfaceVariant { get; }
    public Color InverseSurface { get; }
    public Color InverseOnSurface { get; }
    public Color Outline { get; }
    public Color OutlineVariant { get; }

    /// <summary>
    /// Bar, screen frame and side panels. Translucent when transparency is on.
    /// </summary>
    public Color Layer0 { get; }
    public Color Layer0Base { get; }
    public Color Layer0Border { get; }
    public Color OnLayer0 { get; }

    /// <summary>
    /// Groups on the bar and sections in panels.
    /// </summary>
    public Color Layer1 { get; }
    public Color Layer1Hover { get; }
    public Color Layer1Active { get; }
    public Color OnLayer1 { get; }
    public Color OnLayer1Inactive { get; }

    /// <summary>
    /// Cards inside sections.
    /// </summary>
    public Color Layer2 { get; }
    public Color Layer2Hover { get; }
    public Color Layer2Active { get; }
    public Color Layer2Disabled { get; }
    public Color OnLayer2 { get; }
    public Color OnLayer2Disabled { get; }

    public Color Layer3 { get; }
    public Color Layer3Hover { get; }
    public Color Layer3Active { get; }

    public Color PrimaryHover { get; }
    public Color PrimaryActive { get; }
    public Color PrimaryContainerHover { get; }
    public Color PrimaryContainerActive { get; }
    public Color SecondaryContainerHover { get; }
    public Color SecondaryContainerActive { get; }
    public Color ErrorContainerHover { get; }
    public Color ErrorContainerActive { get; }
    public Color InfoContainerHover { get; }
    public Color InfoContainerActive { get; }

    /// <remarks>
    /// Success colors are fixed in end-4's generator rather than derived from the seed. Layer colors mirror
    /// Appearance.colors of end-4's quickshell config, with extraBackgroundTint on.
    /// </remarks>
    private NebulaPalette(Color seed, bool dark, float backgroundTransparency)
    {
        Seed = seed;
        IsDark = dark;

        var scheme = DynamicScheme.Content(Hct.FromInt(ToArgb(seed)), dark);
        Color Get(DynamicColor role) => FromArgb(role.GetArgb(scheme));

        PrimaryKey = Get(M.PrimaryPaletteKeyColor);

        Primary = Get(M.Primary);
        OnPrimary = Get(M.OnPrimary);
        PrimaryContainer = Get(M.PrimaryContainer);
        OnPrimaryContainer = Get(M.OnPrimaryContainer);
        InversePrimary = Get(M.InversePrimary);

        Secondary = Get(M.Secondary);
        OnSecondary = Get(M.OnSecondary);
        SecondaryContainer = Get(M.SecondaryContainer);
        OnSecondaryContainer = Get(M.OnSecondaryContainer);

        Tertiary = Get(M.Tertiary);
        OnTertiary = Get(M.OnTertiary);
        TertiaryContainer = Get(M.TertiaryContainer);
        OnTertiaryContainer = Get(M.OnTertiaryContainer);

        Error = Get(M.Error);
        OnError = Get(M.OnError);
        ErrorContainer = Get(M.ErrorContainer);
        OnErrorContainer = Get(M.OnErrorContainer);

        Success = Color.FromHex(dark ? "#B5CCBA" : "#4F6354");
        OnSuccess = Color.FromHex(dark ? "#213528" : "#FFFFFF");
        SuccessContainer = Color.FromHex(dark ? "#374B3E" : "#D1E8D5");
        OnSuccessContainer = Color.FromHex(dark ? "#D1E9D6" : "#0C1F13");

        var info = GetCustomColor(InfoSource);
        Info = info.Color;
        OnInfo = info.OnColor;
        InfoContainer = info.Container;
        OnInfoContainer = info.OnContainer;
        InfoContainerHover = info.ContainerHover;
        InfoContainerActive = info.ContainerActive;

        Background = Get(M.Background);
        OnBackground = Get(M.OnBackground);
        Surface = Get(M.Surface);
        SurfaceDim = Get(M.SurfaceDim);
        SurfaceBright = Get(M.SurfaceBright);
        SurfaceContainerLowest = Get(M.SurfaceContainerLowest);
        SurfaceContainerLow = Get(M.SurfaceContainerLow);
        SurfaceContainer = Get(M.SurfaceContainer);
        SurfaceContainerHigh = Get(M.SurfaceContainerHigh);
        SurfaceContainerHighest = Get(M.SurfaceContainerHighest);
        OnSurface = Get(M.OnSurface);
        SurfaceVariant = Get(M.SurfaceVariant);
        OnSurfaceVariant = Get(M.OnSurfaceVariant);
        InverseSurface = Get(M.InverseSurface);
        InverseOnSurface = Get(M.InverseOnSurface);
        Outline = Get(M.Outline);
        OutlineVariant = Get(M.OutlineVariant);

        const float content = 1f - ContentTransparency;

        Layer0Base = Mix(Background, Primary, 0.99f);
        Layer0 = Transparentize(Layer0Base, backgroundTransparency);
        OnLayer0 = OnBackground;
        Layer0Border = Mix(OutlineVariant, Layer0, 0.4f);

        Layer1 = SolveOverlay(Layer0Base, SurfaceContainerLow, content);
        OnLayer1 = OnSurfaceVariant;
        OnLayer1Inactive = Mix(OnLayer1, Layer1, 0.45f);
        Layer1Hover = Transparentize(Mix(Layer1, OnLayer1, 0.92f), ContentTransparency);
        Layer1Active = Transparentize(Mix(Layer1, OnLayer1, 0.85f), ContentTransparency);

        OnLayer2 = OnSurface;
        Layer2 = SolveOverlay(SurfaceContainerLow, SurfaceContainer, content);
        Layer2Hover = SolveOverlay(SurfaceContainerLow, Mix(SurfaceContainer, OnLayer2, 0.9f), content);
        Layer2Active = SolveOverlay(SurfaceContainerLow, Mix(SurfaceContainer, OnLayer2, 0.8f), content);
        Layer2Disabled = SolveOverlay(SurfaceContainerLow, Mix(SurfaceContainer, Background, 0.8f), content);
        OnLayer2Disabled = Mix(OnLayer2, Background, 0.4f);

        Layer3 = SolveOverlay(SurfaceContainer, SurfaceContainerHigh, content);
        Layer3Hover = SolveOverlay(SurfaceContainer, Mix(SurfaceContainerHigh, OnSurface, 0.9f), content);
        Layer3Active = SolveOverlay(SurfaceContainer, Mix(SurfaceContainerHigh, OnSurface, 0.8f), content);

        PrimaryHover = Mix(Primary, Layer1Hover, 0.87f);
        PrimaryActive = Mix(Primary, Layer1Active, 0.7f);
        PrimaryContainerHover = Mix(PrimaryContainer, OnPrimaryContainer, 0.9f);
        PrimaryContainerActive = Mix(PrimaryContainer, OnPrimaryContainer, 0.8f);
        SecondaryContainerHover = Mix(SecondaryContainer, OnSecondaryContainer, 0.9f);
        SecondaryContainerActive = Mix(SecondaryContainer, OnSecondaryContainer, 0.54f);
        ErrorContainerHover = Mix(ErrorContainer, OnErrorContainer, 0.9f);
        ErrorContainerActive = Mix(ErrorContainer, OnErrorContainer, 0.7f);
    }

    /// <summary>
    /// Color roles of <paramref name="source"/> with its hue blended towards <see cref="PrimaryKey"/>,
    /// like Material's custom colors, for things like department colors.
    /// </summary>
    public NebulaCustomColor GetCustomColor(Color source)
    {
        var from = Hct.FromInt(ToArgb(source));
        var to = Hct.FromInt(ToArgb(PrimaryKey));
        var rotation = Math.Min(MaterialMath.DifferenceDegrees(from.Hue, to.Hue) * 0.5, CustomColorMaxRotation);
        var hue = MaterialMath.SanitizeDegrees(from.Hue + rotation * MaterialMath.RotationDirection(from.Hue, to.Hue));
        var palette = TonalPalette.FromHueAndChroma(hue, Math.Max(from.Chroma, CustomColorMinChroma));
        Color Tone(double tone) => FromArgb(palette.Tone(tone));

        var container = Tone(IsDark ? 30 : 90);
        var onContainer = Tone(IsDark ? 90 : 10);

        return new NebulaCustomColor(
            Tone(IsDark ? 80 : 40),
            Tone(IsDark ? 20 : 100),
            container,
            onContainer,
            Mix(container, onContainer, 0.9f),
            Mix(container, onContainer, 0.7f));
    }

    /// <param name="seed">Source color, its alpha is ignored.</param>
    /// <param name="dark">Dark or light scheme.</param>
    /// <param name="backgroundTransparency">How much of what's behind shows through <see cref="Layer0"/>.</param>
    public static NebulaPalette Generate(Color seed, bool dark, float backgroundTransparency = 0f)
    {
        return new NebulaPalette(seed, dark, backgroundTransparency);
    }

    /// <summary>
    /// Shifts the hue of a color that doesn't come from the scheme towards <see cref="PrimaryKey"/> and adjusts its tone
    /// for the mode, the way end-4 tints terminal colors.
    /// </summary>
    public Color Harmonize(Color color)
    {
        var from = Hct.FromInt(ToArgb(color));
        var to = Hct.FromInt(ToArgb(PrimaryKey));
        var rotation = Math.Min(MaterialMath.DifferenceDegrees(from.Hue, to.Hue) * HarmonyAmount, HarmonyThreshold);
        var hue = MaterialMath.SanitizeDegrees(from.Hue + rotation * MaterialMath.RotationDirection(from.Hue, to.Hue));
        var harmonized = Hct.From(hue, from.Chroma, from.Tone);
        var tone = harmonized.Tone * (1 + HarmonyToneBoost * (IsDark ? 1 : -1));
        return FromArgb(Hct.From(harmonized.Hue, harmonized.Chroma, tone).Argb).WithAlpha(color.A);
    }

    /// <summary>
    /// <paramref name="a"/> weighted by <paramref name="amount"/>, including alpha, like end-4's ColorUtils.mix.
    /// </summary>
    public static Color Mix(Color a, Color b, float amount)
    {
        return new Color(
            amount * a.R + (1 - amount) * b.R,
            amount * a.G + (1 - amount) * b.G,
            amount * a.B + (1 - amount) * b.B,
            amount * a.A + (1 - amount) * b.A);
    }

    public static Color Transparentize(Color color, float amount)
    {
        return color.WithAlpha(color.A * (1 - amount));
    }

    /// <summary>
    /// The color that, drawn over <paramref name="baseColor"/> at <paramref name="opacity"/>, gives <paramref name="target"/>.
    /// </summary>
    public static Color SolveOverlay(Color baseColor, Color target, float opacity)
    {
        var inverse = 1f - opacity;

        return new Color(
            Math.Clamp((target.R - baseColor.R * inverse) / opacity, 0f, 1f),
            Math.Clamp((target.G - baseColor.G * inverse) / opacity, 0f, 1f),
            Math.Clamp((target.B - baseColor.B * inverse) / opacity, 0f, 1f),
            opacity);
    }

    private static int ToArgb(Color color)
    {
        return MaterialMath.ArgbFromRgb(color.RByte, color.GByte, color.BByte);
    }

    private static Color FromArgb(int argb)
    {
        return new Color(
            (byte) MaterialMath.RedFromArgb(argb),
            (byte) MaterialMath.GreenFromArgb(argb),
            (byte) MaterialMath.BlueFromArgb(argb));
    }
}

/// <summary>
/// Material custom color roles from <see cref="NebulaPalette.GetCustomColor"/>.
/// </summary>
public readonly record struct NebulaCustomColor(
    Color Color,
    Color OnColor,
    Color Container,
    Color OnContainer,
    Color ContainerHover,
    Color ContainerActive);
