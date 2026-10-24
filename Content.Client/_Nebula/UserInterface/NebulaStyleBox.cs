// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Client.Graphics;

namespace Content.Client._Nebula.UserInterface;

/// <summary>
/// A flat box with rounded corners and an optional border, the base shape of the Nebula interface.
/// </summary>
public sealed class NebulaStyleBox : StyleBox
{
    private const int CornerSegments = 6;
    private const int PathLength = (CornerSegments + 1) * 4;

    // stackalloc is banned on the client, the boxes are drawn on the main thread only
    private readonly Vector2[] _outer = new Vector2[PathLength];
    private readonly Vector2[] _inner = new Vector2[PathLength];
    private readonly Vector2[] _vertices = new Vector2[PathLength * 2 + 2];

    public Color BackgroundColor { get; set; }
    public Color BorderColor { get; set; }
    public float BorderThickness { get; set; }

    /// <summary>
    /// Radius in virtual pixels, clamped to half of the shorter side, so a big value makes a pill.
    /// </summary>
    public float CornerRadius { get; set; }

    public NebulaStyleBox()
    {
    }

    public NebulaStyleBox(Color background, float radius = 0f, Color? border = null, float borderThickness = 1f)
    {
        BackgroundColor = background;
        CornerRadius = radius;
        BorderColor = border ?? Color.Transparent;
        BorderThickness = border == null ? 0f : borderThickness;
    }

    public NebulaStyleBox(NebulaStyleBox other) : base(other)
    {
        BackgroundColor = other.BackgroundColor;
        BorderColor = other.BorderColor;
        BorderThickness = other.BorderThickness;
        CornerRadius = other.CornerRadius;
    }

    /// <summary>
    /// A box with fully rounded ends.
    /// </summary>
    public static NebulaStyleBox Pill(Color background, Color? border = null)
    {
        return new NebulaStyleBox(background, float.MaxValue, border);
    }

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        if (box.Width <= 0 || box.Height <= 0)
            return;

        var radius = MathF.Min(CornerRadius * uiScale, MathF.Min(box.Width, box.Height) / 2f);
        var border = MathF.Min(BorderThickness * uiScale, MathF.Min(box.Width, box.Height) / 2f);

        if (border <= 0f || BorderColor.A <= 0f)
        {
            if (BackgroundColor.A > 0f)
                DrawFill(handle, box, radius, BackgroundColor);

            return;
        }

        var inner = new UIBox2(box.Left + border, box.Top + border, box.Right - border, box.Bottom - border);
        var innerRadius = MathF.Max(0f, radius - border);

        // the ring is drawn separately, so a translucent background doesn't show the border through it
        BuildPath(_outer, box, radius);
        BuildPath(_inner, inner, innerRadius);

        var count = 0;
        for (var i = 0; i < PathLength; i++)
        {
            _vertices[count++] = _outer[i];
            _vertices[count++] = _inner[i];
        }

        _vertices[count++] = _outer[0];
        _vertices[count++] = _inner[0];
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleStrip, _vertices.AsSpan(0, count), BorderColor);

        if (BackgroundColor.A > 0f && inner.Width > 0 && inner.Height > 0)
            DrawFill(handle, inner, innerRadius, BackgroundColor);
    }

    private void DrawFill(DrawingHandleScreen handle, UIBox2 box, float radius, Color color)
    {
        if (radius <= 0f)
        {
            handle.DrawRect(box, color);
            return;
        }

        BuildPath(_outer, box, radius);

        _vertices[0] = box.Center;
        Array.Copy(_outer, 0, _vertices, 1, PathLength);
        _vertices[PathLength + 1] = _outer[0];
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, _vertices.AsSpan(0, PathLength + 2), color);
    }

    /// <summary>
    /// Clockwise outline in screen space, starting at the left end of the top-left corner.
    /// </summary>
    private static void BuildPath(Vector2[] path, UIBox2 box, float radius)
    {
        var index = 0;
        AddCorner(path, ref index, new Vector2(box.Left + radius, box.Top + radius), radius, MathF.PI);
        AddCorner(path, ref index, new Vector2(box.Right - radius, box.Top + radius), radius, MathF.PI * 1.5f);
        AddCorner(path, ref index, new Vector2(box.Right - radius, box.Bottom - radius), radius, 0f);
        AddCorner(path, ref index, new Vector2(box.Left + radius, box.Bottom - radius), radius, MathF.PI * 0.5f);
    }

    private static void AddCorner(Vector2[] path, ref int index, Vector2 center, float radius, float startAngle)
    {
        for (var i = 0; i <= CornerSegments; i++)
        {
            var angle = startAngle + MathF.PI * 0.5f * i / CornerSegments;
            path[index++] = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
        }
    }

    protected override float GetDefaultContentMargin(Margin margin)
    {
        return BorderThickness;
    }
}
