// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Nebula.UserInterface.Blur;
using Content.Client._Nebula.UserInterface.Shapes;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Nebula.UserInterface.Controls;

/// <summary>
/// Box container drawn as a filled <see cref="ShapeStyle"/> with an optional border.
/// </summary>
public sealed class ShapedPanel : BoxContainer
{
    public const string StylePropertyBackgroundColor = "shapedPanelBackgroundColor";
    public const string StylePropertyBorderColor = "shapedPanelBorderColor";
    public const string StylePropertyBorderThickness = "shapedPanelBorderThickness";
    public const string StylePropertyShape = "shapedPanelShape";

    private readonly ShapeOutline _outline = new();
    private readonly DrawVertexUV2D[] _backdropVertices = new DrawVertexUV2D[ShapeGeometry.PointCount];

    private Color? _backgroundColor;
    private Color? _borderColor;
    private float? _borderThickness;
    private ShapeStyle? _shape;

    public Color BackgroundColor
    {
        get => _backgroundColor ?? this.GetStyle(StylePropertyBackgroundColor, Color.Transparent);
        set => _backgroundColor = value;
    }

    public Color BorderColor
    {
        get => _borderColor ?? this.GetStyle(StylePropertyBorderColor, Color.Transparent);
        set => _borderColor = value;
    }

    public float BorderThickness
    {
        get => _borderThickness ?? this.GetStyle(StylePropertyBorderThickness, 0f);
        set => _borderThickness = value;
    }

    public ShapeStyle Shape
    {
        get => _shape ?? this.GetStyle(StylePropertyShape, ShapeStyle.Square);
        set => _shape = value;
    }

    /// <summary>
    /// When set, the panel shows a blurred image of this source behind its background color.
    /// </summary>
    public IBackdropSource? Backdrop { get; set; }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (PixelSize.X <= 0 || PixelSize.Y <= 0)
            return;

        var border = BorderThickness * UIScale;
        _outline.Update(PixelSizeBox, Shape, UIScale, border);

        var hasBorder = border > 0f && BorderColor.A > 0f;
        var body = hasBorder ? _outline.Inner : _outline.Outer;

        DrawBackdrop(handle, body);
        ShapeGeometry.DrawFill(handle, body, BackgroundColor);

        if (hasBorder)
            ShapeGeometry.DrawRing(handle, _outline.Outer, _outline.Inner, BorderColor);
    }

    private void DrawBackdrop(DrawingHandleScreen handle, Vector2[] outline)
    {
        if (Backdrop?.BackdropTexture is not { } texture)
            return;

        var rect = Backdrop.BackdropScreenRect;

        if (rect.Width <= 0f || rect.Height <= 0f)
            return;

        var origin = (Vector2) GlobalPixelPosition;

        for (var i = 0; i < outline.Length; i++)
        {
            var screen = origin + outline[i];
            var u = (screen.X - rect.Left) / rect.Width;
            var v = (screen.Y - rect.Top) / rect.Height;
            _backdropVertices[i] = new DrawVertexUV2D(outline[i], new Vector2(u, 1f - v));
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, texture, _backdropVertices);
    }
}
