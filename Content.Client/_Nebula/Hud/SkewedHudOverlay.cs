// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Content.Client._Nebula.Death;
using Content.Client.Resources;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Actions.Widgets;
using Content.Client.UserInterface.Systems.Alerts.Widgets;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.Mobs;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._Nebula.Hud;

/// <summary>
/// Draws the security neural interface HUD under the top menu bar, right next to the action buttons: squad, info and
/// tasks sections of <c>LABEL :: value</c> lines on faint plates. The whole column is sheared along Y so it falls
/// to the right while glyphs stay upright. A service header with the shift time sits on top, plates carry faint
/// scanlines. With the separated chat the tasks move to the top right corner, mirrored. Sections slide in one by one when the HUD is turned on,
/// and a random line glitches now and then. Hovering a task with a description shows it in a tooltip.
/// </summary>
public sealed class SkewedHudOverlay : Overlay
{
    private const string ScrambleChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789#%@$&";
    private const float MarginX = 6f;
    private const float MarginY = 10f;
    private const float PaddingX = 10f;
    private const float PaddingY = 6f;
    private const float SectionGap = 7f;
    private const float AccentWidth = 2f;
    private const float SeparatorGap = 6f;
    private const float DotSize = 3f;
    private const float GlowRadius = 1.5f;
    private const float PulseSpeed = 4f;
    private const float DecryptTime = 0.3f;
    private const float FlatlineDelay = 0.25f;
    private const float FlatlineStagger = 0.4f;
    private const float FlatlineBreakTime = 0.5f;
    private const float FlatlineFadeTime = 1.5f;
    private const float FlatlineNoise = 0.12f;
    private const float BootTime = 0.25f;
    private const float BootStagger = 0.1f;
    private const float BootShift = 16f;
    private const float GlitchMinInterval = 6f;
    private const float GlitchMaxInterval = 10f;
    private const float GlitchShift = 2f;
    private const float GlitchGhostOffset = 1.5f;
    private const float HeaderGap = 4f;
    private const float ScanlineStep = 3f;
    private const float ScanlineAlpha = 0.04f;
    private const float LagNoise = 0.35f;
    private const float LagMinInterval = 0.15f;
    private const float LagMaxInterval = 0.5f;
    private const float LagGlitchChance = 0.06f;
    private const float LagFlickerChance = 0.1f;
    private const float BannerTop = 0.18f;
    private const float BannerPaddingX = 16f;
    private const float BannerPaddingY = 6f;
    private const float BannerCut = 6f;
    private const float BannerIconSize = 30f;
    private const float BannerIconGap = 10f;
    private const float BannerIconTime = 0.6f;
    private const float BannerOpenTime = 0.3f;
    private const float BannerHoldTime = 4f;
    private const float BannerCloseTime = 0.25f;
    private const float BannerCollapseTime = 0.3f;
    private const float BannerFlickerChance = 0.1f;
    private const float TooltipWidth = 300f;
    private const float TooltipOffset = 16f;
    private const float TooltipPadding = 8f;
    private static readonly TimeSpan GlitchTime = TimeSpan.FromMilliseconds(60);
    private static readonly float SkewAngle = MathHelper.DegreesToRadians(3f);

    private static readonly Color DefaultAccent = Color.FromHex("#3fd8ff");
    private static readonly Color DefaultAlert = Color.FromHex("#ff4040");
    private static readonly Color BannerColor = Color.FromHex("#f2625c");
    private static readonly Color BannerTextColor = Color.FromHex("#4a0d0d");
    private static readonly Color PlateColor = Color.FromHex("#061018").WithAlpha(0.35f);
    private static readonly Color TooltipColor = Color.FromHex("#061018").WithAlpha(0.9f);

    private static readonly Vector2[] GlowOffsets =
    [
        new(1f, 0f), new(-1f, 0f), new(0f, 1f), new(0f, -1f),
    ];

    private readonly SkewedHudSystem _hud;
    private readonly ISharedPlayerManager _player;
    private readonly IEyeManager _eye;
    private readonly IUserInterfaceManager _ui;
    private readonly IInputManager _input;
    private readonly IGameTiming _timing;
    private readonly IRobustRandom _random;
    private readonly Font _headerFont;
    private readonly Font _labelFont;
    private readonly Font _valueFont;
    private readonly Font _bannerFont;
    private readonly Font _tooltipFont;
    private readonly List<Section> _sections = [];
    private readonly List<Row> _rows = [];
    private readonly StringBuilder _decrypted = new();
    private readonly DrawVertexUV2DColor[] _plate = new DrawVertexUV2DColor[4];
    private readonly DrawVertexUV2DColor[] _shape = new DrawVertexUV2DColor[5];
    private readonly List<DrawVertexUV2DColor> _scanlines = [];
    private TimeSpan? _bannerFrom;
    private int _builtVersion = -1;
    private float _builtScale;
    private bool _builtSplit;
    private TimeSpan _nextGlitch;
    private int _headerMinutes = -1;
    private string _header = string.Empty;
    private TimeSpan _glitchUntil;
    private int _glitchRow;
    private float _glitchShift;

    /// <summary>
    /// Row with a description under the mouse this frame, -1 when none.
    /// </summary>
    private int _hovered = -1;

    /// <summary>
    /// Wrapped description of <see cref="_tooltipRow"/>, rebuilt when the rows or the scale change.
    /// </summary>
    private readonly List<string> _tooltipLines = [];
    private int _tooltipRow = -1;
    private int _tooltipVersion = -1;
    private float _tooltipScale;
    private Vector2 _tooltipSize;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private Color Accent => _hud.Profile?.Accent ?? DefaultAccent;
    private Color AccentDim => _hud.Profile?.AccentDim ?? DefaultAccent.WithAlpha(0.65f);
    private Color Alert => _hud.Profile?.Alert ?? DefaultAlert;

    public SkewedHudOverlay(
        SkewedHudSystem hud,
        ISharedPlayerManager player,
        IEyeManager eye,
        IUserInterfaceManager ui,
        IInputManager input,
        IResourceCache cache,
        IGameTiming timing,
        IRobustRandom random)
    {
        _hud = hud;
        _player = player;
        _eye = eye;
        _ui = ui;
        _input = input;
        _timing = timing;
        _random = random;
        _headerFont = cache.GetFont("/Fonts/RobotoMono/RobotoMono-Bold.ttf", 10);
        _labelFont = cache.GetFont("/Fonts/Boxfont-round/Boxfont Round.ttf", 10);
        _valueFont = cache.GetFont("/Fonts/RobotoMono/RobotoMono-Bold.ttf", 13);
        _bannerFont = cache.GetFont("/Fonts/RobotoMono/RobotoMono-Bold.ttf", 15);
        _tooltipFont = cache.GetFont("/Fonts/RobotoMono/RobotoMono-Regular.ttf", 11);
        ZIndex = NebulaDeathOverlay.ZOrder + 1;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _hud.Active
            && _player.LocalEntity != null
            && args.ViewportControl == _eye.MainViewport
            && FlatlineFade() > 0f;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.ScreenHandle;
        var bounds = args.ViewportBounds;
        var scale = (args.ViewportControl as Control)?.UIScale ?? 1f;

        // The overlay is drawn under the UI, so the HUD follows the action buttons: right of the column,
        // or under the row with the separated chat.
        var anchor = Vector2.Zero;
        var rightAnchor = new Vector2(bounds.Right, bounds.Top);
        var split = _ui.ActiveScreen is SeparatedChatGameScreen;
        if (_ui.ActiveScreen is { } screen)
        {
            if (screen.GetWidget<AlertsUI>() is { VisibleInTree: true } alerts)
                rightAnchor.X = Math.Min(rightAnchor.X, alerts.GlobalPixelRect.Left);

            if (screen.GetWidget<ActionsBar>()?.ActionsContainer is { VisibleInTree: true } buttons
                && TryGetButtonsRect(buttons, out var rect))
                anchor = split ? new Vector2(rect.Left, rect.Bottom) : new Vector2(rect.Right, rect.Top);
            else if (!split && screen.GetWidget<GameTopMenuBar>() is { VisibleInTree: true } topBar)
                anchor.Y = topBar.GlobalPixelRect.Bottom;
        }

        if (_hud.Version != _builtVersion
            || scale != _builtScale
            || split != _builtSplit)
        {
            _builtVersion = _hud.Version;
            _builtScale = scale;
            _builtSplit = split;
            _headerMinutes = -1;
            BuildRows(split);
            Measure(handle, scale);
        }

        UpdateGlitch();

        var mouse = _input.MouseScreenPosition.Position;
        _hovered = -1;

        var left = Matrix3x2.CreateSkew(0f, SkewAngle) * Matrix3x2.CreateTranslation(anchor + new Vector2(MarginX, MarginY) * scale);
        handle.SetTransform(left);
        var leftMouse = ToLocal(left, mouse);

        var y = DrawHeader(handle, scale);
        for (var s = 0; s < _sections.Count; s++)
        {
            if (!_sections[s].Right)
                y += DrawSection(handle, _sections[s], s + 1, y, scale, leftMouse) + SectionGap * scale;
        }

        var right = Matrix3x2.CreateSkew(0f, -SkewAngle) * Matrix3x2.CreateTranslation(rightAnchor + new Vector2(-MarginX, MarginY) * scale);
        handle.SetTransform(right);
        var rightMouse = ToLocal(right, mouse);

        var rightY = 0f;
        for (var s = 0; s < _sections.Count; s++)
        {
            if (_sections[s].Right)
                rightY += DrawSection(handle, _sections[s], s + 1, rightY, scale, rightMouse) + SectionGap * scale;
        }

        handle.SetTransform(Matrix3x2.Identity);
        if (_hovered >= 0)
            DrawTooltip(handle, bounds, mouse, scale);

        DrawSignalBanner(handle, bounds, scale);
    }

    #region Build

    /// <param name="split">Separated chat leaves the top right corner free, so the tasks go there.</param>
    private void BuildRows(bool split)
    {
        _sections.Clear();
        _rows.Clear();

        var next = 0;
        for (; next < _hud.Sections.Count && _hud.Sections[next].Order < SkewedHudSystem.InfoOrder; next++)
        {
            AddSection(_hud.Sections[next]);
        }

        var info = _rows.Count;
        if (_hud.Location != null)
            AddRow(Loc.GetString("skewed-hud-location"), _hud.Location, Accent, _hud.LocationChangedAt, lags: true);

        AddRow(Loc.GetString("skewed-hud-status"), StateName(_hud.SelfState), SkewedHudSystem.StateColor(_hud.SelfState), _hud.SelfStateChangedAt);

        if (_hud.AlertName != null)
            AddRow(Loc.GetString("skewed-hud-code"), _hud.AlertName, _hud.AlertColor, _hud.AlertChangedAt, lags: true);

        EndSection(info);

        for (; next < _hud.Sections.Count; next++)
        {
            AddSection(_hud.Sections[next]);
        }

        if (_hud.Tasks.Count > 0)
        {
            var start = _rows.Count;
            var visible = Math.Min(_hud.Tasks.Count, SkewedHudSystem.MaxVisibleTasks);
            for (var i = 0; i < visible; i++)
            {
                var task = _hud.Tasks[i];
                if (task.Urgent)
                    AddRow(task.Label, task.Text, Alert, task.ChangedAt, Alert.WithAlpha(0.65f), true, description: task.Description);
                else
                    AddRow(task.Label, task.Text, Accent, task.ChangedAt, description: task.Description);
            }

            if (_hud.Tasks.Count > visible)
            {
                var more = Loc.GetString("skewed-hud-tasks-more", ("count", _hud.Tasks.Count - visible));
                AddRow(Loc.GetString("skewed-hud-tasks-more-label"), more, AccentDim, TimeSpan.Zero);
            }

            EndSection(start, split);
        }
    }

    private void AddSection(SkewedHudSection section)
    {
        var start = _rows.Count;
        foreach (var line in section.Lines)
        {
            AddRow(line.Label,
                line.Value,
                line.Color ?? Accent,
                line.ChangedAt,
                line.LabelColor,
                line.Pulse,
                line.Lags);
        }

        EndSection(start, section.Right);
    }

    private void AddRow(string label,
        string text,
        Color color,
        TimeSpan changedAt,
        Color? labelColor = null,
        bool pulse = false,
        bool lags = false,
        string? description = null)
    {
        _rows.Add(new Row
        {
            Description = description,
            Label = label,
            LabelColor = labelColor ?? AccentDim,
            Text = text,
            Color = color,
            ChangedAt = changedAt,
            Pulse = pulse,
            Lags = lags,
        });
    }

    private void EndSection(int start, bool right = false)
    {
        _sections.Add(new Section { Start = start, Count = _rows.Count - start, Right = right });
    }

    private static string StateName(MobState state)
    {
        return state switch
        {
            MobState.Dead => Loc.GetString("skewed-hud-state-dead"),
            MobState.SoftCrit or MobState.Critical => Loc.GetString("skewed-hud-state-crit"),
            _ => Loc.GetString("skewed-hud-state-alive"),
        };
    }

    #endregion

    #region Layout

    /// <summary>
    /// Fills row widths, then section columns and sizes. The <c>::</c> column follows the longest label of the section
    /// with explicit gaps, because the label font has a near-zero space width.
    /// </summary>
    private void Measure(DrawingHandleScreen handle, float scale)
    {
        var lineHeight = Math.Max(_labelFont.GetLineHeight(scale), _valueFont.GetLineHeight(scale));
        for (var i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            row.LabelWidth = InkWidth(_labelFont, row.Label, scale);
            row.TextWidth = handle.GetDimensions(_valueFont, row.Text, scale).X;
            _rows[i] = row;
        }

        var gap = SeparatorGap * scale;
        var separatorWidth = DotSize * 3 * scale;
        for (var s = 0; s < _sections.Count; s++)
        {
            var section = _sections[s];
            for (var i = section.Start; i < section.Start + section.Count; i++)
            {
                section.SeparatorColumn = Math.Max(section.SeparatorColumn, _rows[i].LabelWidth);
            }

            section.SeparatorColumn += gap;
            section.ValueColumn = section.SeparatorColumn + separatorWidth + gap;
            section.Size.Y = lineHeight * section.Count;
            for (var i = section.Start; i < section.Start + section.Count; i++)
            {
                section.Size.X = Math.Max(section.Size.X, section.ValueColumn + _rows[i].TextWidth);
            }

            _sections[s] = section;
        }
    }

    /// <summary>
    /// Width up to the right edge of the last glyph. Boxfont glyphs overhang their advance, so measuring by advance
    /// leaves the longest label touching the separator.
    /// </summary>
    private static float InkWidth(Font font, string text, float scale)
    {
        var advance = 0f;
        var ink = 0f;
        foreach (var rune in text.EnumerateRunes())
        {
            if (font.GetCharMetrics(rune, scale) is not { } metrics)
                continue;

            ink = Math.Max(ink, advance + metrics.BearingX + metrics.Width);
            advance += metrics.Advance;
        }

        return Math.Max(advance, ink);
    }

    /// <summary>
    /// Bounds of the visible action buttons. The container is capped at one button width, so it can't be used itself.
    /// </summary>
    private static bool TryGetButtonsRect(Control buttons, out UIBox2 rect)
    {
        rect = default;
        var found = false;
        foreach (var button in buttons.Children)
        {
            if (!button.Visible)
                continue;

            UIBox2 box = button.GlobalPixelRect;
            rect = found
                ? new UIBox2(Math.Min(rect.Left, box.Left), Math.Min(rect.Top, box.Top), Math.Max(rect.Right, box.Right), Math.Max(rect.Bottom, box.Bottom))
                : box;
            found = true;
        }

        return found;
    }

    #endregion

    #region Drawing

    /// <summary>
    /// Draws a section plate with its rows and returns the plate height. While the HUD boots, the section fades
    /// and slides in after the previous ones, and its rows decrypt once it has arrived.
    /// </summary>
    /// <param name="mouse">Mouse position in the coordinates of the section, to find the hovered row.</param>
    private float DrawSection(DrawingHandleScreen handle, Section section, int index, float top, float scale, Vector2? mouse)
    {
        var delay = BootStagger * index;
        var boot = BootProgress(index);
        var slide = BootShift * scale * (1f - boot) * (1f - boot) * (section.Right ? 1f : -1f);
        var revealFrom = _hud.ActivatedAt + TimeSpan.FromSeconds(delay + BootTime);

        var padX = PaddingX * scale;
        var padY = PaddingY * scale;
        var width = section.Size.X + padX * 2;
        var plate = UIBox2.FromDimensions(slide - (section.Right ? width : 0f), top, width, section.Size.Y + padY * 2);
        DrawPlate(handle, plate, boot, scale, section.Right);

        var accent = AccentWidth * scale;
        var bar = section.Right
            ? new UIBox2(plate.Right - accent, plate.Top, plate.Right, plate.Bottom)
            : new UIBox2(plate.Left, plate.Top, plate.Left + accent, plate.Bottom);
        handle.DrawRect(bar, Fade(Accent, boot));

        var lineHeight = section.Size.Y / section.Count;
        var valueHeight = _valueFont.GetHeight(scale);
        var labelOffset = (valueHeight - _labelFont.GetHeight(scale)) / 2f;
        var glitching = _timing.RealTime < _glitchUntil;
        var lost = _hud.SignalLostAt != null && _hud.FlatlineAt == null;
        var pulse = 0.4f + 0.3f * (MathF.Sin((float) _timing.RealTime.TotalSeconds * PulseSpeed) + 1f);
        var y = plate.Top + padY;
        for (var i = section.Start; i < section.Start + section.Count; i++)
        {
            var row = _rows[i];
            var broken = FlatlineBreak(i);
            if (broken >= 1f)
            {
                y += lineHeight;
                continue;
            }

            if (row.Description != null
                && broken == null
                && !lost
                && boot >= 1f
                && mouse is { } m
                && m.X >= plate.Left
                && m.X <= plate.Right
                && m.Y >= y
                && m.Y < y + lineHeight)
                _hovered = i;

            var lagging = lost && row.Lags;
            var glitch = (glitching && i == _glitchRow) || broken >= 0f || lagging && _random.Prob(LagGlitchChance);
            var x = plate.Left + padX + (glitch ? _glitchShift * scale : 0f);
            var labelY = y + labelOffset;
            var value = broken switch
            {
                null when lagging => Lag(i),
                null => Decrypt(row.Text, row.ChangedAt > revealFrom ? row.ChangedAt : revealFrom),
                < 0f => Shatter(row.Text, FlatlineNoise),
                _ => Shatter(row.Text, 0.5f + broken.Value * 0.5f),
            };
            var valuePos = new Vector2(x + section.ValueColumn, y);
            var alpha = row.Pulse ? boot * pulse : boot;
            if (lagging && _random.Prob(LagFlickerChance))
                alpha *= 0.35f;

            DrawGlowString(handle, _labelFont, new Vector2(x, labelY), row.Label, Fade(row.LabelColor, alpha), scale);
            DrawSeparator(handle, new Vector2(x + section.SeparatorColumn, y + valueHeight / 2f), Fade(AccentDim, boot), scale);
            if (glitch)
            {
                var ghost = new Vector2(GlitchGhostOffset * scale, 0f);
                handle.DrawString(_valueFont, valuePos + ghost, value, scale, Alert.WithAlpha(0.5f));
                handle.DrawString(_valueFont, valuePos - ghost, value, scale, Accent.WithAlpha(0.5f));
            }

            DrawGlowString(handle, _valueFont, valuePos, value, Fade(row.Color, alpha), scale);

            y += lineHeight;
        }

        return plate.Height;
    }

    /// <summary>
    /// Draws <c>::</c> as four square dots around the line center. The label font puts its colon near the baseline,
    /// which looks sunk next to the taller value font.
    /// </summary>
    private static void DrawSeparator(DrawingHandleScreen handle, Vector2 leftCenter, Color color, float scale)
    {
        var size = DotSize * scale;
        for (var column = 0; column < 2; column++)
        {
            var left = leftCenter.X + column * size * 2;
            handle.DrawRect(new UIBox2(left, leftCenter.Y - size * 1.5f, left + size, leftCenter.Y - size * 0.5f), color);
            handle.DrawRect(new UIBox2(left, leftCenter.Y + size * 0.5f, left + size, leftCenter.Y + size * 1.5f), color);
        }
    }

    /// <summary>
    /// Draws the header of the profile, like <c>NT-SEC // ... hh:mm</c>, and returns the height it takes.
    /// The text is rebuilt only when the shift minute or the HUD data changes.
    /// </summary>
    private float DrawHeader(DrawingHandleScreen handle, float scale)
    {
        var minutes = Math.Max(0, (int) _hud.ShiftTime.TotalMinutes);
        if (minutes != _headerMinutes)
        {
            _headerMinutes = minutes;
            _header = _hud.Profile is { } profile
                ? Loc.GetString(profile.Header, ("time", $"{minutes / 60:D2}:{minutes % 60:D2}"))
                : string.Empty;
        }

        var boot = BootProgress(0);
        var pos = new Vector2(PaddingX * scale, 0f);
        var height = _headerFont.GetHeight(scale);
        DrawGlowString(handle, _headerFont, pos, _header, Fade(AccentDim, boot), scale);

        return height + HeaderGap * scale;
    }

    private float BootProgress(int index)
    {
        var elapsed = (float) (_timing.RealTime - _hud.ActivatedAt).TotalSeconds - BootStagger * index;
        return Math.Clamp(elapsed / BootTime, 0f, 1f) * FlatlineFade();
    }

    /// <summary>
    /// After death, once every row broke up, the plates and the header fade out.
    /// </summary>
    private float FlatlineFade()
    {
        if (_hud.FlatlineAt is not { } dead)
            return 1f;

        var broken = FlatlineDelay + FlatlineStagger * _rows.Count + FlatlineBreakTime;
        var elapsed = (float) (_timing.RealTime - dead).TotalSeconds - broken;
        return 1f - Math.Clamp(elapsed / FlatlineFadeTime, 0f, 1f);
    }

    /// <summary>
    /// How far the row has broken up after death: null while alive, below 0 before its turn, 1 once gone.
    /// Rows go bottom to top.
    /// </summary>
    private float? FlatlineBreak(int row)
    {
        if (_hud.FlatlineAt is not { } dead)
            return null;

        var start = FlatlineDelay + FlatlineStagger * (_rows.Count - 1 - row);
        return ((float) (_timing.RealTime - dead).TotalSeconds - start) / FlatlineBreakTime;
    }

    /// <summary>
    /// Replaces each character with a random one at the given chance.
    /// </summary>
    private string Shatter(string text, float chance)
    {
        _decrypted.Clear();
        foreach (var c in text)
        {
            _decrypted.Append(char.IsWhiteSpace(c) || _random.NextFloat() >= chance
                ? c
                : ScrambleChars[_random.Next(ScrambleChars.Length)]);
        }

        return _decrypted.ToString();
    }

    /// <summary>
    /// The row's text broken up, rerolled in jerks rather than every frame so it reads as a stalling feed.
    /// </summary>
    private string Lag(int index)
    {
        var row = _rows[index];
        if (row.LagText != null && _timing.RealTime < row.LagNext)
            return row.LagText;

        row.LagText = Shatter(row.Text, LagNoise);
        row.LagNext = _timing.RealTime + TimeSpan.FromSeconds(_random.NextFloat(LagMinInterval, LagMaxInterval));
        _rows[index] = row;
        return row.LagText;
    }

    /// <summary>
    /// Plate that fades out away from its accent bar, so it reads as a projection rather than a box, with faint scanlines.
    /// </summary>
    private void DrawPlate(DrawingHandleScreen handle, UIBox2 box, float alpha, float scale, bool mirrored)
    {
        DrawFadingRect(handle, box, Fade(PlateColor, alpha), mirrored);

        var scanline = Fade(Accent.WithAlpha(ScanlineAlpha), alpha);
        var left = mirrored ? scanline.WithAlpha(0f) : scanline;
        var right = mirrored ? scanline : scanline.WithAlpha(0f);
        _scanlines.Clear();
        for (var y = box.Top; y < box.Bottom; y += ScanlineStep * scale)
        {
            var line = new UIBox2(box.Left, y, box.Right, y + scale);
            _scanlines.Add(new DrawVertexUV2DColor(line.TopLeft, left));
            _scanlines.Add(new DrawVertexUV2DColor(line.BottomLeft, left));
            _scanlines.Add(new DrawVertexUV2DColor(line.TopRight, right));
            _scanlines.Add(new DrawVertexUV2DColor(line.TopRight, right));
            _scanlines.Add(new DrawVertexUV2DColor(line.BottomLeft, left));
            _scanlines.Add(new DrawVertexUV2DColor(line.BottomRight, right));
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, Texture.White, CollectionsMarshal.AsSpan(_scanlines));
    }

    /// <summary>
    /// Rectangle with <paramref name="color"/> on one edge fading to transparent on the other: left to right,
    /// or right to left when <paramref name="mirrored"/>.
    /// </summary>
    private void DrawFadingRect(DrawingHandleScreen handle, UIBox2 box, Color color, bool mirrored)
    {
        var left = mirrored ? color.WithAlpha(0f) : color;
        var right = mirrored ? color : color.WithAlpha(0f);
        _plate[0] = new DrawVertexUV2DColor(box.TopLeft, left);
        _plate[1] = new DrawVertexUV2DColor(box.BottomLeft, left);
        _plate[2] = new DrawVertexUV2DColor(box.TopRight, right);
        _plate[3] = new DrawVertexUV2DColor(box.BottomRight, right);
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleStrip, Texture.White, _plate);
    }

    private static Vector2? ToLocal(Matrix3x2 transform, Vector2 screen)
    {
        return Matrix3x2.Invert(transform, out var inverse) ? Vector2.Transform(screen, inverse) : null;
    }

    /// <summary>
    /// Description of the hovered task on an upright plate next to the mouse, kept inside the viewport.
    /// </summary>
    private void DrawTooltip(DrawingHandleScreen handle, UIBox2 bounds, Vector2 mouse, float scale)
    {
        if (_rows[_hovered].Description is not { } description)
            return;

        if (_tooltipRow != _hovered
            || _tooltipVersion != _builtVersion
            || _tooltipScale != scale)
        {
            _tooltipRow = _hovered;
            _tooltipVersion = _builtVersion;
            _tooltipScale = scale;
            WrapTooltip(handle, description, scale);
        }

        var pad = TooltipPadding * scale;
        var size = _tooltipSize + new Vector2(pad * 2);
        var position = mouse + new Vector2(TooltipOffset * scale);
        position.X = Math.Clamp(position.X, bounds.Left, Math.Max(bounds.Left, bounds.Right - size.X));
        position.Y = Math.Clamp(position.Y, bounds.Top, Math.Max(bounds.Top, bounds.Bottom - size.Y));

        var box = UIBox2.FromDimensions(position, size);
        handle.DrawRect(box, TooltipColor);
        handle.DrawRect(new UIBox2(box.Left, box.Top, box.Left + AccentWidth * scale, box.Bottom), Accent);

        var lineHeight = _tooltipFont.GetLineHeight(scale);
        var y = box.Top + pad;
        foreach (var line in _tooltipLines)
        {
            handle.DrawString(_tooltipFont, new Vector2(box.Left + pad, y), line, scale, Accent);
            y += lineHeight;
        }
    }

    /// <summary>
    /// Splits the description by words into lines no wider than <see cref="TooltipWidth"/>, a longer word keeps
    /// a line of its own.
    /// </summary>
    private void WrapTooltip(DrawingHandleScreen handle, string text, float scale)
    {
        _tooltipLines.Clear();
        var maxWidth = TooltipWidth * scale;
        var width = 0f;
        foreach (var paragraph in text.Split('\n'))
        {
            var line = string.Empty;
            foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && handle.GetDimensions(_tooltipFont, candidate, scale).X > maxWidth)
                {
                    _tooltipLines.Add(line);
                    line = word;
                }
                else
                    line = candidate;
            }

            _tooltipLines.Add(line);
        }

        foreach (var line in _tooltipLines)
        {
            width = Math.Max(width, handle.GetDimensions(_tooltipFont, line, scale).X);
        }

        _tooltipSize = new Vector2(width, _tooltipFont.GetLineHeight(scale) * _tooltipLines.Count);
    }

    private void UpdateGlitch()
    {
        var now = _timing.RealTime;
        if (now < _nextGlitch
            || _rows.Count == 0)
            return;

        _nextGlitch = now + TimeSpan.FromSeconds(_random.NextFloat(GlitchMinInterval, GlitchMaxInterval));
        _glitchUntil = now + GlitchTime;
        _glitchRow = _random.Next(_rows.Count);
        _glitchShift = _random.Prob(0.5f) ? GlitchShift : -GlitchShift;
    }

    private static Color Fade(Color color, float alpha)
    {
        return color.WithAlpha(color.A * alpha);
    }

    /// <summary>
    /// Shortly after a change, reveals the text left to right over random characters.
    /// </summary>
    private string Decrypt(string text, TimeSpan changedAt)
    {
        var progress = (float) (_timing.RealTime - changedAt).TotalSeconds / DecryptTime;
        if (progress >= 1f)
            return text;

        var revealed = (int) (text.Length * progress);
        _decrypted.Clear();
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            _decrypted.Append(i < revealed || char.IsWhiteSpace(c) ? c : ScrambleChars[_random.Next(ScrambleChars.Length)]);
        }

        return _decrypted.ToString();
    }

    private static void DrawGlowString(DrawingHandleScreen handle, Font font, Vector2 pos, string text, Color color, float scale)
    {
        var glow = color.WithAlpha(color.A * 0.08f);
        var radius = GlowRadius * scale;
        foreach (var offset in GlowOffsets)
        {
            handle.DrawString(font, pos + offset * radius, text, scale, glow);
        }

        handle.DrawString(font, pos, text, scale, color);
    }

    #endregion

    #region Signal banner

    /// <summary>
    /// Warning over the top of the viewport when the link drops: the sign blinks alone, then a red plate grows out of it
    /// with the text typing in. After a few seconds, or once the link is back, the plate shrinks into a blinking square.
    /// </summary>
    private void DrawSignalBanner(DrawingHandleScreen handle, UIBox2 bounds, float scale)
    {
        if (_hud.SignalLostAt is { } lostAt)
            _bannerFrom = lostAt;

        if (_bannerFrom is not { } from
            || _hud.FlatlineAt != null)
            return;

        // The HUD was cleared without the link coming back, there is nothing to close.
        if (_hud.SignalLostAt == null && _hud.SignalRestoredAt < from)
        {
            _bannerFrom = null;
            return;
        }

        var elapsed = (float) (_timing.RealTime - from).TotalSeconds;
        var closeAt = BannerIconTime + BannerOpenTime + BannerHoldTime;
        if (_hud.SignalLostAt == null && _hud.SignalRestoredAt > from)
            closeAt = Math.Min(closeAt, (float) (_hud.SignalRestoredAt - from).TotalSeconds);

        var open = EaseOut(Math.Clamp((Math.Min(elapsed, closeAt) - BannerIconTime) / BannerOpenTime, 0f, 1f));
        var closing = elapsed - closeAt;
        if (closing > BannerCloseTime + (open > 0f ? BannerCollapseTime : 0f))
        {
            _bannerFrom = null;
            return;
        }

        var shrink = 1f - Math.Clamp(closing / BannerCloseTime, 0f, 1f);
        var title = Loc.GetString("skewed-hud-signal-lost");
        var titleSize = handle.GetDimensions(_bannerFont, title, scale);
        var padX = BannerPaddingX * scale;
        var icon = BannerIconSize * scale;
        var gap = BannerIconGap * scale;
        var height = titleSize.Y + BannerPaddingY * scale * 2;
        var stub = height * 0.9f;
        var fullWidth = titleSize.X + padX * 2;
        var top = bounds.Top + bounds.Height * BannerTop;

        if (closing > BannerCloseTime)
        {
            DrawCollapse(handle, new Vector2(bounds.Center.X + (icon + gap) / 2f, top + height / 2f), stub, height,
                (closing - BannerCloseTime) / BannerCollapseTime);
            return;
        }

        var reveal = open * shrink;
        var width = reveal > 0f ? stub + (fullWidth - stub) * reveal : 0f;
        var left = bounds.Center.X - (icon + (width > 0f ? gap + width : 0f)) / 2f;
        var animating = reveal is > 0f and < 1f;

        var iconAlpha = Math.Clamp(elapsed / 0.15f, 0f, 1f) * shrink;
        if (_random.Prob(elapsed < BannerIconTime ? BannerFlickerChance * 2f : BannerFlickerChance))
            iconAlpha *= 0.35f;

        DrawWarningSign(handle, new Vector2(left + icon / 2f, top + height / 2f), icon, scale, iconAlpha);
        if (width <= 0f)
            return;

        var plate = UIBox2.FromDimensions(left + icon + gap, top, width, height);
        DrawBannerPlate(handle, plate, BannerCut * scale, scale);

        var room = Math.Clamp((width - padX * 2) / titleSize.X, 0f, 1f);
        var text = BannerText(title, (int) (title.Length * room), animating);
        var textPos = new Vector2(plate.Left + padX, plate.Top + BannerPaddingY * scale);
        if (animating && _random.Prob(0.5f))
        {
            var ghost = new Vector2(GlitchGhostOffset * scale, 0f);
            handle.DrawString(_bannerFont, textPos + ghost, text, scale, BannerTextColor.WithAlpha(0.4f));
        }

        handle.DrawString(_bannerFont, textPos, text, scale, BannerTextColor);
        if (reveal >= 1f)
        {
            handle.DrawString(_headerFont,
                new Vector2(plate.Left + 2f * scale, plate.Bottom + 3f * scale),
                Loc.GetString("skewed-hud-signal-lost-sub", ("tag", _hud.Profile?.Tag ?? string.Empty)),
                scale,
                BannerColor.WithAlpha(0.7f));
        }
    }

    /// <summary>
    /// The visible part of the title, its last characters scrambled while the plate moves.
    /// </summary>
    private string BannerText(string title, int count, bool animating)
    {
        _decrypted.Clear();
        for (var i = 0; i < count; i++)
        {
            var c = title[i];
            _decrypted.Append(animating && i >= count - 2 && !char.IsWhiteSpace(c)
                ? ScrambleChars[_random.Next(ScrambleChars.Length)]
                : c);
        }

        return _decrypted.ToString();
    }

    /// <summary>
    /// Solid plate with the bottom right corner cut, a faint glow around it and a thin line inside its left edge.
    /// </summary>
    private void DrawBannerPlate(DrawingHandleScreen handle, UIBox2 box, float cut, float scale)
    {
        for (var i = 2; i >= 1; i--)
        {
            var grow = i * 2f * scale;
            FillBannerShape(handle, new UIBox2(box.Left - grow, box.Top - grow, box.Right + grow, box.Bottom + grow),
                cut + grow, BannerColor.WithAlpha(0.12f));
        }

        FillBannerShape(handle, box, cut, BannerColor);
        var line = box.Left + 5f * scale;
        handle.DrawRect(new UIBox2(line, box.Top + 3f * scale, line + scale, box.Bottom - 3f * scale), BannerTextColor.WithAlpha(0.35f));
    }

    private void FillBannerShape(DrawingHandleScreen handle, UIBox2 box, float cut, Color color)
    {
        _shape[0] = new DrawVertexUV2DColor(box.TopLeft, color);
        _shape[1] = new DrawVertexUV2DColor(box.TopRight, color);
        _shape[2] = new DrawVertexUV2DColor(new Vector2(box.Right, box.Bottom - cut), color);
        _shape[3] = new DrawVertexUV2DColor(new Vector2(box.Right - cut, box.Bottom), color);
        _shape[4] = new DrawVertexUV2DColor(box.BottomLeft, color);
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, Texture.White, _shape);
    }

    /// <summary>
    /// What is left of the plate narrows into a blinking square and goes out.
    /// </summary>
    private void DrawCollapse(DrawingHandleScreen handle, Vector2 center, float stub, float height, float progress)
    {
        if (_random.Prob(0.3f))
            return;

        var half = new Vector2(stub + (height * 0.3f - stub) * progress, height * (1f - 0.5f * progress)) / 2f;
        handle.DrawRect(new UIBox2(center - half, center + half), BannerColor.WithAlpha(1f - progress * 0.6f));
    }

    /// <summary>
    /// Outlined warning sign with an exclamation mark and short ticks above and below its corners.
    /// </summary>
    private void DrawWarningSign(DrawingHandleScreen handle, Vector2 center, float size, float scale, float alpha)
    {
        if (alpha <= 0f)
            return;

        var height = size * MathF.Sqrt(3f) / 2f;
        var centroid = center + new Vector2(0f, height / 6f);
        var inradius = height / 3f;
        var thickness = 2f * scale;
        var color = Fade(BannerColor, alpha);
        DrawTriangleFrame(handle, centroid, size, height, (inradius + 1.5f * scale) / inradius,
            (inradius - thickness - 1.5f * scale) / inradius, color.WithAlpha(color.A * 0.2f));
        DrawTriangleFrame(handle, centroid, size, height, 1f, (inradius - thickness) / inradius, color);

        var bar = 2.5f * scale;
        var markTop = centroid.Y - inradius * 0.95f;
        var markBottom = centroid.Y + inradius * 0.05f;
        handle.DrawRect(new UIBox2(center.X - bar / 2f, markTop, center.X + bar / 2f, markBottom), color);
        handle.DrawRect(new UIBox2(center.X - bar / 2f, markBottom + bar, center.X + bar / 2f, markBottom + bar * 2f), color);

        var tick = 3f * scale;
        var above = centroid.Y - height * 2f / 3f - 3f * scale;
        var below = centroid.Y + height / 3f + 2f * scale;
        DrawTick(center.X - tick - scale, above);
        DrawTick(center.X + scale, above);
        DrawTick(center.X - size * 0.35f, below);
        DrawTick(center.X + size * 0.35f - tick, below);

        void DrawTick(float x, float y)
        {
            handle.DrawRect(new UIBox2(x, y, x + tick, y + scale), color);
        }
    }

    /// <summary>
    /// Band between two copies of the upward triangle around <paramref name="centroid"/>, scaled by
    /// <paramref name="outer"/> and <paramref name="inner"/>. Each side is its own quad, so the corners stay mitered.
    /// </summary>
    private void DrawTriangleFrame(DrawingHandleScreen handle,
        Vector2 centroid,
        float size,
        float height,
        float outer,
        float inner,
        Color color)
    {
        for (var i = 0; i < 3; i++)
        {
            var from = TriangleCorner(i, size, height);
            var to = TriangleCorner((i + 1) % 3, size, height);
            _plate[0] = new DrawVertexUV2DColor(centroid + from * outer, color);
            _plate[1] = new DrawVertexUV2DColor(centroid + to * outer, color);
            _plate[2] = new DrawVertexUV2DColor(centroid + to * inner, color);
            _plate[3] = new DrawVertexUV2DColor(centroid + from * inner, color);
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, Texture.White, _plate);
        }
    }

    private static Vector2 TriangleCorner(int index, float size, float height)
    {
        return index switch
        {
            0 => new Vector2(0f, -height * 2f / 3f),
            1 => new Vector2(size / 2f, height / 3f),
            _ => new Vector2(-size / 2f, height / 3f),
        };
    }

    private static float EaseOut(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    #endregion

    private struct Row
    {
        public string? Description;
        public string Label;
        public Color LabelColor;
        public string Text;
        public Color Color;
        public TimeSpan ChangedAt;
        public bool Pulse;
        public bool Lags;
        public string? LagText;
        public TimeSpan LagNext;
        public float LabelWidth;
        public float TextWidth;
    }

    private struct Section
    {
        public int Start;
        public int Count;
        public bool Right;
        public float SeparatorColumn;
        public float ValueColumn;
        public Vector2 Size;
    }
}
