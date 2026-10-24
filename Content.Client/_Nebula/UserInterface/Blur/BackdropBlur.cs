// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._Nebula.UserInterface.Blur;

/// <summary>
/// Renders a heavily blurred, downscaled copy of a texture region using a separable gaussian shader.
/// </summary>
public sealed partial class BackdropBlur : IDisposable
{
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    private static readonly ProtoId<ShaderPrototype> BlurShader = "NebulaGaussianBlur";

    private readonly ShaderInstance _shader;

    private IRenderTexture? _primary;
    private IRenderTexture? _secondary;

    /// <summary>
    /// How many times smaller than the output the blurred image is rendered.
    /// </summary>
    public int Downscale { get; set; } = 8;

    /// <summary>
    /// Number of horizontal and vertical blur pass pairs.
    /// </summary>
    public int Iterations { get; set; } = 3;

    /// <summary>
    /// Distance between blur samples in texels of the downscaled image.
    /// </summary>
    public float Spread { get; set; } = 1f;

    public Texture? Result => _primary?.Texture;

    public BackdropBlur()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototype.Index(BlurShader).InstanceUnique();
    }

    /// <summary>
    /// Blurs <paramref name="sourceRegion"/> of <paramref name="source"/> into <see cref="Result"/>.
    /// </summary>
    /// <param name="handle">Handle of the control currently being drawn.</param>
    /// <param name="sourceRegion">Region of the texture in texture pixels.</param>
    /// <param name="outputSize">Size in pixels the result will be displayed at.</param>
    public void Render(DrawingHandleScreen handle, Texture source, UIBox2 sourceRegion, Vector2i outputSize)
    {
        var downscale = Math.Max(1, Downscale);
        var size = new Vector2i(Math.Max(1, outputSize.X / downscale), Math.Max(1, outputSize.Y / downscale));

        EnsureTargets(size);

        var primary = _primary!;
        var secondary = _secondary!;
        var box = UIBox2.FromDimensions(Vector2.Zero, size);
        var horizontal = new Vector2(Spread / size.X, 0f);
        var vertical = new Vector2(0f, Spread / size.Y);
        var transform = handle.GetTransform();

        handle.SetTransform(Matrix3x2.Identity);
        handle.RenderInRenderTarget(primary, () => handle.DrawTextureRectRegion(source, box, sourceRegion), Color.Transparent);

        for (var i = 0; i < Iterations; i++)
        {
            Pass(handle, primary, secondary, box, horizontal);
            Pass(handle, secondary, primary, box, vertical);
        }

        handle.SetTransform(transform);
    }

    public void Dispose()
    {
        _primary?.Dispose();
        _secondary?.Dispose();
        _primary = null;
        _secondary = null;
    }

    private void Pass(DrawingHandleScreen handle, IRenderTexture from, IRenderTexture to, UIBox2 box, Vector2 step)
    {
        _shader.SetParameter("BLUR_STEP", step);

        handle.RenderInRenderTarget(to, () =>
        {
            handle.UseShader(_shader);
            handle.DrawTextureRect(from.Texture, box);
            handle.UseShader(null);
        }, Color.Transparent);
    }

    private void EnsureTargets(Vector2i size)
    {
        if (_primary != null && _primary.Size == size)
            return;

        Dispose();

        var format = new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb);
        var sampling = new TextureSampleParameters { Filter = true };

        _primary = _clyde.CreateRenderTarget(size, format, sampling, nameof(BackdropBlur));
        _secondary = _clyde.CreateRenderTarget(size, format, sampling, nameof(BackdropBlur));
    }
}
