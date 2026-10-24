// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Client.Graphics;

namespace Content.Client._Nebula.UserInterface.Shapes;

/// <summary>
/// Builds outlines for <see cref="ShapeStyle"/>. Every outline has exactly <see cref="PointCount"/> points,
/// so any two outlines can be joined into a ring regardless of their corner kinds.
/// </summary>
public static class ShapeGeometry
{
    public const int PointsPerCorner = 9;
    public const int PointCount = PointsPerCorner * 4;

    private static readonly float ChamferInsetFactor = 2f - MathF.Sqrt(2f);

    private static readonly Vector2[] Directions = BuildDirections();

    private static readonly DrawVertexUV2DColor[] FillVertices = new DrawVertexUV2DColor[PointCount];
    private static readonly DrawVertexUV2DColor[] RingStrip = new DrawVertexUV2DColor[(PointCount + 1) * 2];

    /// <summary>
    /// Fills <paramref name="points"/> with the outline of <paramref name="box"/>.
    /// </summary>
    /// <param name="points">Destination, at least <see cref="PointCount"/> long.</param>
    /// <param name="box">Rectangle in pixels.</param>
    /// <param name="shape">Corner description in virtual pixels.</param>
    /// <param name="scale">UI scale used to convert corner sizes to pixels.</param>
    /// <param name="inset">Distance in pixels to shrink the outline inward, keeping corners concentric.</param>
    public static void Build(Vector2[] points, UIBox2 box, ShapeStyle shape, float scale, float inset = 0f)
    {
        var maxSize = MathF.Max(0f, MathF.Min(box.Width, box.Height) / 2f);
        var inner = new UIBox2(box.Left + inset, box.Top + inset, box.Right - inset, box.Bottom - inset);

        BuildCorner(points, 0, shape.TopLeft, new Vector2(inner.Left, inner.Top), 1, 1, scale, maxSize, inset);
        BuildCorner(points, 1, shape.TopRight, new Vector2(inner.Right, inner.Top), -1, 1, scale, maxSize, inset);
        BuildCorner(points, 2, shape.BottomRight, new Vector2(inner.Right, inner.Bottom), -1, -1, scale, maxSize, inset);
        BuildCorner(points, 3, shape.BottomLeft, new Vector2(inner.Left, inner.Bottom), 1, -1, scale, maxSize, inset);
    }

    /// <summary>
    /// Fills the outline as a convex polygon.
    /// </summary>
    /// <remarks>
    /// Uses colored vertices, the Vector2 overload of DrawPrimitives converts the color from sRGB, which StyleBoxFlat
    /// doesn't do, so shapes would come out darker than the same palette color.
    /// </remarks>
    public static void DrawFill(DrawingHandleScreen handle, Vector2[] outline, Color color)
    {
        if (color.A <= 0f)
            return;

        color *= handle.Modulate;

        for (var i = 0; i < PointCount; i++)
        {
            FillVertices[i] = new DrawVertexUV2DColor(outline[i], color);
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, Texture.White, FillVertices);
    }

    /// <summary>
    /// Fills the area between two outlines built with <see cref="Build"/>.
    /// </summary>
    public static void DrawRing(DrawingHandleScreen handle, Vector2[] outer, Vector2[] inner, Color color)
    {
        if (color.A <= 0f)
            return;

        color *= handle.Modulate;

        var index = 0;

        for (var i = 0; i < PointCount; i++)
        {
            RingStrip[index++] = new DrawVertexUV2DColor(outer[i], color);
            RingStrip[index++] = new DrawVertexUV2DColor(inner[i], color);
        }

        RingStrip[index++] = new DrawVertexUV2DColor(outer[0], color);
        RingStrip[index] = new DrawVertexUV2DColor(inner[0], color);

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleStrip, Texture.White, RingStrip);
    }

    private static void BuildCorner(
        Vector2[] points,
        int cornerIndex,
        Corner corner,
        Vector2 vertex,
        float signX,
        float signY,
        float scale,
        float maxSize,
        float inset)
    {
        var size = corner.Kind == CornerKind.Square
            ? 0f
            : MathF.Min(corner.Size * scale, maxSize);

        size = corner.Kind switch
        {
            CornerKind.Round => size - inset,
            CornerKind.Chamfer => size - inset * ChamferInsetFactor,
            _ => size,
        };

        size = MathF.Max(0f, size);

        var offset = cornerIndex * PointsPerCorner;
        var center = vertex + new Vector2(signX * size, signY * size);

        if (corner.Kind != CornerKind.Chamfer)
        {
            for (var i = 0; i < PointsPerCorner; i++)
            {
                points[offset + i] = center + Directions[offset + i] * size;
            }

            return;
        }

        var start = center + Directions[offset] * size;
        var end = center + Directions[offset + PointsPerCorner - 1] * size;

        for (var i = 0; i < PointsPerCorner; i++)
        {
            points[offset + i] = Vector2.Lerp(start, end, i / (float) (PointsPerCorner - 1));
        }
    }

    private static Vector2[] BuildDirections()
    {
        var directions = new Vector2[PointCount];

        for (var corner = 0; corner < 4; corner++)
        {
            var startAngle = MathF.PI + corner * MathF.PI / 2f;

            for (var i = 0; i < PointsPerCorner; i++)
            {
                var angle = startAngle + MathF.PI / 2f * (i / (float) (PointsPerCorner - 1));
                directions[corner * PointsPerCorner + i] = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            }
        }

        return directions;
    }
}
