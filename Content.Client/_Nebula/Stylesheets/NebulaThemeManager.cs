// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Stylesheets;
using Content.Shared._Nebula.CCVar;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;

namespace Content.Client._Nebula.Stylesheets;

/// <summary>
/// Owns the current <see cref="NebulaPalette"/> and rebuilds the stylesheets when its settings change.
/// </summary>
public sealed partial class NebulaThemeManager
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IStylesheetManager _stylesheets = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    private bool _rebuildQueued;

    private Stylesheet? _scopedBase;
    private NebulaPalette? _scopedPalette;
    private Stylesheet? _scoped;

    public NebulaPalette Palette { get; private set; } = default!;

    /// <summary>
    /// Raised after the stylesheets are rebuilt, for colors set from code.
    /// </summary>
    public event Action<NebulaPalette>? PaletteChanged;

    /// <summary>
    /// Must run before <see cref="IStylesheetManager.Initialize"/>: Nebula sheetlets read <see cref="Palette"/>.
    /// </summary>
    public void Initialize()
    {
        Palette = CreatePalette();

        _cfg.OnValueChanged(NCCVars.InterfaceAccent, _ => QueueRebuild());
        _cfg.OnValueChanged(NCCVars.InterfaceTransparency, _ => QueueRebuild());
        _cfg.OnValueChanged(NCCVars.InterfaceTransparencyAmount, _ => QueueRebuild());
    }

    /// <summary>
    /// Falls back to the default accent on invalid input.
    /// </summary>
    public static Color ParseSeed(string hex)
    {
        return TryParseSeed(hex, out var color) ? color : Color.FromHex(NCCVars.InterfaceAccent.DefaultValue);
    }

    /// <summary>
    /// The leading # is optional.
    /// </summary>
    public static bool TryParseSeed(string text, out Color color)
    {
        text = text.Trim();

        if (!text.StartsWith('#'))
            text = "#" + text;

        return Color.TryFromHex(text, out color);
    }

    public static string FormatSeed(Color color)
    {
        return $"#{color.RByte:X2}{color.GByte:X2}{color.BByte:X2}";
    }

    private void QueueRebuild()
    {
        // Applying options sets several CVars at once, rebuild only once per frame.
        if (_rebuildQueued)
            return;

        _rebuildQueued = true;
        _ui.DeferAction(Rebuild);
    }

    /// <summary>
    /// <see cref="NebulaScopedStylesheet"/> over <paramref name="baseSheet"/>, cached until the base sheet or palette changes.
    /// Kept per client: integration tests run several clients in one process.
    /// </summary>
    public Stylesheet GetScopedStylesheet(Stylesheet baseSheet)
    {
        if (_scoped != null && ReferenceEquals(baseSheet, _scopedBase) && ReferenceEquals(Palette, _scopedPalette))
            return _scoped;

        _scopedBase = baseSheet;
        _scopedPalette = Palette;
        _scoped = NebulaScopedStylesheet.Create(baseSheet, Palette);
        return _scoped;
    }

    private void Rebuild()
    {
        _rebuildQueued = false;
        Palette = CreatePalette();
        ((StylesheetManager) _stylesheets).Reload();
        PaletteChanged?.Invoke(Palette);
    }

    private NebulaPalette CreatePalette()
    {
        var transparency = _cfg.GetCVar(NCCVars.InterfaceTransparency)
            ? Math.Clamp(_cfg.GetCVar(NCCVars.InterfaceTransparencyAmount), 0f, 1f)
            : 0f;

        return NebulaPalette.Generate(
            ParseSeed(_cfg.GetCVar(NCCVars.InterfaceAccent)),
            dark: true,
            transparency);
    }
}
