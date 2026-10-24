// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Graphics;

namespace Content.Client._Nebula.UserInterface.Shapes;

/// <summary>
/// Style box drawn with <see cref="ShapeGeometry"/>, so vanilla controls that only take a <see cref="StyleBox"/>
/// get the same corners and colors as <see cref="Controls.ShapedPanel"/>.
/// </summary>
public sealed class ShapedStyleBox : StyleBox
{
    private readonly ShapeOutline _outline = new();

    public Color BackgroundColor { get; set; } = Color.Transparent;

    public Color BorderColor { get; set; } = Color.Transparent;

    /// <summary>
    /// Border thickness in virtual pixels.
    /// </summary>
    public float BorderThickness { get; set; }

    public ShapeStyle Shape { get; set; } = ShapeStyle.Square;

    /// <summary>
    /// Shrinks the drawn shape inside its box, in virtual pixels, for thin slider tracks and the like.
    /// </summary>
    public Thickness Inset { get; set; }

    public ShapedStyleBox()
    {
    }

    public ShapedStyleBox(ShapedStyleBox other) : base(other)
    {
        BackgroundColor = other.BackgroundColor;
        BorderColor = other.BorderColor;
        BorderThickness = other.BorderThickness;
        Shape = other.Shape;
        Inset = other.Inset;
    }

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        box = new UIBox2(
            box.Left + Inset.Left * uiScale,
            box.Top + Inset.Top * uiScale,
            box.Right - Inset.Right * uiScale,
            box.Bottom - Inset.Bottom * uiScale);

        if (box.Width <= 0f || box.Height <= 0f)
            return;

        var border = BorderThickness * uiScale;
        _outline.Update(box, Shape, uiScale, border);

        if (border <= 0f || BorderColor.A <= 0f)
        {
            ShapeGeometry.DrawFill(handle, _outline.Outer, BackgroundColor);
            return;
        }

        ShapeGeometry.DrawFill(handle, _outline.Inner, BackgroundColor);
        ShapeGeometry.DrawRing(handle, _outline.Outer, _outline.Inner, BorderColor);
    }
}
