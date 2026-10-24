// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Nebula.UserInterface.Blur;
using Content.Client._Nebula.UserInterface.Shapes;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client._Nebula.UserInterface.Controls;

public enum ShapedTextureStretch : byte
{
    Cover,
    Fill,
}

/// <summary>
/// Draws a texture clipped to a <see cref="ShapeStyle"/>. With <see cref="BlurEnabled"/> it also keeps a blurred
/// copy of what it shows, so translucent panels above it can use it as a backdrop.
/// </summary>
public sealed class ShapedTextureRect : Control, IBackdropSource
{
    public const string StylePropertyShape = "shapedTextureShape";

    private readonly ShapeOutline _outline = new();
    private readonly DrawVertexUV2D[] _vertices = new DrawVertexUV2D[ShapeGeometry.PointCount];

    private BackdropBlur? _blur;
    private Texture? _blurredTexture;
    private Vector2i _blurredSize;
    private bool _blurValid;

    public Texture? Texture { get; set; }

    public ShapedTextureStretch Stretch { get; set; } = ShapedTextureStretch.Cover;

    public bool BlurEnabled { get; set; }

    public Texture? BackdropTexture => _blurValid ? _blur?.Result : null;

    public UIBox2 BackdropScreenRect => GlobalPixelRect;

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (Texture is not { } texture || PixelSize.X <= 0 || PixelSize.Y <= 0)
        {
            _blurValid = false;
            return;
        }

        var width = (float) PixelSize.X;
        var height = (float) PixelSize.Y;
        var (uvMin, uvMax) = GetUv(texture.Size, width, height);

        if (BlurEnabled)
            UpdateBlur(handle, texture, uvMin, uvMax);

        _outline.Update(PixelSizeBox, this.GetStyle(StylePropertyShape, ShapeStyle.Square), UIScale, 0f);

        var outline = _outline.Outer;

        for (var i = 0; i < outline.Length; i++)
        {
            var point = outline[i];
            var u = uvMin.X + point.X / width * (uvMax.X - uvMin.X);
            var v = uvMin.Y + point.Y / height * (uvMax.Y - uvMin.Y);
            _vertices[i] = new DrawVertexUV2D(point, new Vector2(u, 1f - v));
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, texture, _vertices);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        _blur?.Dispose();
        _blur = null;
        _blurValid = false;
    }

    private void UpdateBlur(DrawingHandleScreen handle, Texture texture, Vector2 uvMin, Vector2 uvMax)
    {
        if (_blurValid && ReferenceEquals(_blurredTexture, texture) && _blurredSize == PixelSize)
            return;

        _blur ??= new BackdropBlur();

        var region = new UIBox2(uvMin * texture.Size, uvMax * texture.Size);
        _blur.Render(handle, texture, region, PixelSize);

        _blurredTexture = texture;
        _blurredSize = PixelSize;
        _blurValid = true;
    }

    private (Vector2 Min, Vector2 Max) GetUv(Vector2i textureSize, float width, float height)
    {
        if (Stretch == ShapedTextureStretch.Fill || textureSize.X <= 0 || textureSize.Y <= 0)
            return (Vector2.Zero, Vector2.One);

        var controlAspect = width / height;
        var textureAspect = (float) textureSize.X / textureSize.Y;

        if (textureAspect > controlAspect)
        {
            var margin = (1f - controlAspect / textureAspect) / 2f;
            return (new Vector2(margin, 0f), new Vector2(1f - margin, 1f));
        }

        var verticalMargin = (1f - textureAspect / controlAspect) / 2f;
        return (new Vector2(0f, verticalMargin), new Vector2(1f, 1f - verticalMargin));
    }
}
