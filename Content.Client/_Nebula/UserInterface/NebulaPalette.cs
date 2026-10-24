// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Client._Nebula.UserInterface;

/// <summary>
/// Colors of the Nebula interface, taken from the main lobby.
/// </summary>
public static class NebulaPalette
{
    /// <summary>
    /// Deepest background, window frames and insets.
    /// </summary>
    public static readonly Color Void = Color.FromHex("#0A0714");

    /// <summary>
    /// Raised translucent container, the base of panels.
    /// </summary>
    public static readonly Color Surface = Color.FromHex("#3C2A5CE8");

    /// <summary>
    /// Window headers and bars.
    /// </summary>
    public static readonly Color SurfaceHeader = Color.FromHex("#342350E0");

    /// <summary>
    /// Idle buttons and secondary backgrounds.
    /// </summary>
    public static readonly Color SurfaceLow = Color.FromHex("#342450ED");

    /// <summary>
    /// Disabled controls and failing states.
    /// </summary>
    public static readonly Color Status = Color.FromHex("#860024");

    public static readonly Color PurpleDim = Color.FromHex("#4A2F78");
    public static readonly Color Purple = Color.FromHex("#6D3FB5");
    public static readonly Color PurpleBright = Color.FromHex("#9A62E8");

    public static readonly Color MagentaDim = Color.FromHex("#5C2F63");
    public static readonly Color Magenta = Color.FromHex("#C15FC9");

    public static readonly Color Hover = Color.FromHex("#6C43B1");
    public static readonly Color Pressed = Color.FromHex("#55396E");

    /// <summary>
    /// Borders of containers and headers.
    /// </summary>
    public static readonly Color Border = Color.FromHex("#6E4C9C");

    /// <summary>
    /// Main text color.
    /// </summary>
    public static readonly Color Moonlight = Color.FromHex("#CBC0F0");

    /// <summary>
    /// Captions, hints and placeholders.
    /// </summary>
    public static readonly Color MoonlightMuted = Moonlight.WithAlpha(0.6f);

    /// <summary>
    /// <see cref="Status"/> brightened enough to read as an accent on the dark surfaces.
    /// </summary>
    public static readonly Color Danger = Color.FromHex("#D23A5F");

    /// <summary>
    /// Marked favorites.
    /// </summary>
    public static readonly Color Gold = Color.FromHex("#E8B04A");
}
