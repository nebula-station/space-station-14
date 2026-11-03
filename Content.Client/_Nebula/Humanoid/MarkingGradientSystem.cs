// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client.Stylesheets.Colorspace;
using Content.Shared._Nebula.Humanoid.Markings;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.Humanoid;

/// <summary>
/// Gives marking sprite layers their own instance of the gradient shader.
/// </summary>
public sealed partial class MarkingGradientSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public static readonly ProtoId<ShaderPrototype> ShaderId = "NebulaMarkingGradient";

    /// <summary>
    /// Part of a frame the head occupies, from the bottom of the frame. Other layers use the whole frame.
    /// </summary>
    private static readonly Vector2 HeadRegion = new(0.45f, 0.95f);

    private static readonly Vector2 FullRegion = new(0f, 1f);

    private static readonly HashSet<HumanoidVisualLayers> HeadLayers =
    [
        HumanoidVisualLayers.Hair,
        HumanoidVisualLayers.HairSpecial,
        HumanoidVisualLayers.FacialHair,
        HumanoidVisualLayers.FacialHairSpecial,
        HumanoidVisualLayers.Head,
        HumanoidVisualLayers.HeadSide,
        HumanoidVisualLayers.HeadTop,
        HumanoidVisualLayers.Snout,
        HumanoidVisualLayers.SnoutCover,
        HumanoidVisualLayers.Eyes,
        HumanoidVisualLayers.Face,
    ];

    /// <summary>
    /// Replaces the shader of a marking's sprite layer with the gradient, if the marking has one.
    /// States with a shader of their own in the prototype keep it. The first color moves into the shader,
    /// and only its alpha stays on the layer so translucent markings stay translucent.
    /// </summary>
    public void Apply(
        Entity<SpriteComponent> sprite,
        int layer,
        MarkingPrototype proto,
        Marking marking,
        int spriteIndex,
        SpriteSpecifier.Rsi rsi)
    {
        if (marking.Gradient is not { } gradient
            || proto.Shaders?.ContainsKey(rsi.RsiState) == true)
            return;

        var safe = gradient.Sanitized();
        var roots = spriteIndex < marking.MarkingColors.Count ? marking.MarkingColors[spriteIndex] : Color.White;
        var state = _sprite.GetState(rsi);

        var shader = _proto.Index(ShaderId).InstanceUnique();
        shader.SetParameter("rootsLab", ToOklab(roots));
        shader.SetParameter("accentLab", ToOklab(safe.Color));
        shader.SetParameter("gradientStyle", (int) safe.Style);
        shader.SetParameter("amount", safe.Offset);
        shader.SetParameter("region", HeadLayers.Contains(proto.BodyPart) ? HeadRegion : FullRegion);
        shader.SetParameter("frameSize", (Vector2) state.Size);
        shader.SetParameter("displaced", false);

        // Only frame 0 of each direction fits in a uniform, so an animated state would flicker between its views.
        var directional = state.RsiDirections != RsiDirectionType.Dir1 && !state.IsAnimated;
        shader.SetParameter("frameNorth", directional ? FrameUv(state, RsiDirection.North) : Vector4.Zero);
        shader.SetParameter("frameEast", directional ? FrameUv(state, RsiDirection.East) : Vector4.Zero);
        shader.SetParameter("frameWest", directional ? FrameUv(state, RsiDirection.West) : Vector4.Zero);

        _sprite.LayerSetColor(sprite.AsNullable(), layer, Color.White.WithAlpha(roots.A));
        sprite.Comp.LayerSetShader(layer, shader, ShaderId.Id);
    }

    /// <summary>
    /// Disposes the layer's gradient shader, if it has one, right before the layer goes away.
    /// </summary>
    public void Remove(Entity<SpriteComponent> sprite, int layer)
    {
        if (sprite.Comp[layer] is not SpriteComponent.Layer { Shader: { } shader } spriteLayer
            || !IsGradientLayer(spriteLayer))
            return;

        sprite.Comp.LayerSetShader(layer, (ShaderInstance?) null);
        shader.Dispose();
    }

    public static bool IsGradientLayer(SpriteComponent.Layer layer)
    {
        return layer.ShaderPrototype == ShaderId.Id;
    }

    /// <summary>
    /// Turns on displacement in the gradient shader, because a layer can only hold one shader.
    /// </summary>
    /// <returns>False if the layer doesn't use the gradient shader.</returns>
    public static bool TrySetDisplaced(SpriteComponent.Layer layer)
    {
        if (!IsGradientLayer(layer) || layer.Shader is not { } shader)
            return false;

        shader.SetParameter("displaced", true);
        return true;
    }

    private static Vector3 ToOklab(Color color)
    {
        var lab = color.LabFromSrgb();
        return new Vector3(lab.X, lab.Y, lab.Z);
    }

    /// <summary>
    /// Returns the frame's rectangle in the atlas as the shader sees it in UV: min x, min y, max x, max y.
    /// </summary>
    private static Vector4 FrameUv(RSI.State state, RsiDirection direction)
    {
        if (state.GetFrame(direction, 0) is not AtlasTexture frame)
            return Vector4.Zero;

        var (width, height) = frame.SourceTexture.Size;
        var region = frame.SubRegion;
        return new Vector4(
            region.Left / width,
            (height - region.Bottom) / height,
            region.Right / width,
            (height - region.Top) / height);
    }
}
