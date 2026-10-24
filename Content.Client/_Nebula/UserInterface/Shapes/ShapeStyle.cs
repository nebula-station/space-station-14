// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Client._Nebula.UserInterface.Shapes;

public enum CornerKind : byte
{
    Square,
    Round,
    Chamfer,
}

/// <summary>
/// A single corner of a <see cref="ShapeStyle"/>.
/// </summary>
/// <param name="Kind">How the corner is cut.</param>
/// <param name="Size">Radius or chamfer length in virtual pixels. <see cref="float.PositiveInfinity"/> clamps to half of the shorter side.</param>
public readonly record struct Corner(CornerKind Kind, float Size)
{
    public static readonly Corner Square = new(CornerKind.Square, 0f);

    public static Corner Round(float radius) => new(CornerKind.Round, radius);

    public static Corner Chamfer(float size) => new(CornerKind.Chamfer, size);
}

/// <summary>
/// Outline of a rectangle with configurable corners, shared by all shaped controls.
/// </summary>
public readonly record struct ShapeStyle(Corner TopLeft, Corner TopRight, Corner BottomRight, Corner BottomLeft)
{
    public static readonly ShapeStyle Square = Uniform(Corner.Square);

    public static readonly ShapeStyle Pill = Rounded(float.PositiveInfinity);

    public static ShapeStyle Uniform(Corner corner) => new(corner, corner, corner, corner);

    public static ShapeStyle Rounded(float radius) => Uniform(Corner.Round(radius));

    /// <summary>
    /// The Nanotrasen angled rectangle: top-right and bottom-left corners are cut, the others are square.
    /// </summary>
    public static ShapeStyle Angled(float size) =>
        new(Corner.Square, Corner.Chamfer(size), Corner.Square, Corner.Chamfer(size));
}
