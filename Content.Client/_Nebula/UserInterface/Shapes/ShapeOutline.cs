// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;

namespace Content.Client._Nebula.UserInterface.Shapes;

/// <summary>
/// Caches the outer outline of a shape and an inset copy for its border, rebuilding only when inputs change.
/// </summary>
public sealed class ShapeOutline
{
    private readonly Vector2[] _outer = new Vector2[ShapeGeometry.PointCount];
    private readonly Vector2[] _inner = new Vector2[ShapeGeometry.PointCount];

    private UIBox2 _box;
    private ShapeStyle _shape;
    private float _scale;
    private float _inset;
    private bool _valid;

    public Vector2[] Outer => _outer;

    public Vector2[] Inner => _inner;

    /// <param name="box">Rectangle in pixels.</param>
    /// <param name="shape">Corner description in virtual pixels.</param>
    /// <param name="inset">Border thickness in pixels, used for <see cref="Inner"/>.</param>
    public void Update(UIBox2 box, ShapeStyle shape, float scale, float inset)
    {
        if (_valid && _box.Equals(box) && _shape == shape && _scale.Equals(scale) && _inset.Equals(inset))
            return;

        _box = box;
        _shape = shape;
        _scale = scale;
        _inset = inset;
        _valid = true;

        ShapeGeometry.Build(_outer, box, shape, scale);
        ShapeGeometry.Build(_inner, box, shape, scale, inset);
    }
}
