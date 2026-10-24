// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Nebula.UserInterface.Shapes;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client._Nebula.UserInterface.Controls;

/// <summary>
/// Fills its rectangle except for a shaped hole, like a screen frame with rounded inner corners.
/// </summary>
public sealed class CutoutFrame : Control
{
    public const string StylePropertyBackgroundColor = "cutoutFrameBackgroundColor";
    public const string StylePropertyBorderColor = "cutoutFrameBorderColor";
    public const string StylePropertyBorderThickness = "cutoutFrameBorderThickness";
    public const string StylePropertyHoleShape = "cutoutFrameHoleShape";

    private readonly Vector2[] _outer = new Vector2[ShapeGeometry.PointCount];
    private readonly ShapeOutline _hole = new();

    /// <summary>
    /// Distance in virtual pixels from each edge to the hole.
    /// </summary>
    public Thickness HoleInset { get; set; }

    public CutoutFrame()
    {
        MouseFilter = MouseFilterMode.Ignore;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (PixelSize.X <= 0 || PixelSize.Y <= 0)
            return;

        var scale = UIScale;
        var background = this.GetStyle(StylePropertyBackgroundColor, Color.Transparent);
        var border = this.GetStyle(StylePropertyBorderColor, Color.Transparent);
        var thickness = this.GetStyle(StylePropertyBorderThickness, 0f) * scale;

        var hole = new UIBox2(
            HoleInset.Left * scale,
            HoleInset.Top * scale,
            PixelSize.X - HoleInset.Right * scale,
            PixelSize.Y - HoleInset.Bottom * scale);

        ShapeGeometry.Build(_outer, PixelSizeBox, ShapeStyle.Square, scale);

        if (hole.Width <= 0f || hole.Height <= 0f)
        {
            ShapeGeometry.DrawFill(handle, _outer, background);
            return;
        }

        _hole.Update(hole, this.GetStyle(StylePropertyHoleShape, ShapeStyle.Square), scale, thickness);

        ShapeGeometry.DrawRing(handle, _outer, _hole.Outer, background);

        if (thickness > 0f)
            ShapeGeometry.DrawRing(handle, _hole.Outer, _hole.Inner, border);
    }
}
