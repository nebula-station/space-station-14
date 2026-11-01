// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Containers;
using Robust.Shared.Enums;

namespace Content.Client._Nebula.Hud;

/// <summary>
/// Draws the outlines of <see cref="SecurityAllyOutlineSystem"/>. It runs after FOV has been applied, so walls do not hide them.
/// </summary>
public sealed partial class SecurityAllyOutlineOverlay : Overlay
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IEyeManager _eye = default!;

    private readonly SpriteSystem _sprite;
    private readonly SharedTransformSystem _transform;
    private readonly SharedContainerSystem _container;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    /// <summary>
    /// Bodies to outline, each with the single-pass shader list of its health step.
    /// </summary>
    public readonly Dictionary<EntityUid, SpriteComponent.PostShaderEntry[]> Allies = [];

    public SecurityAllyOutlineOverlay()
    {
        IoCManager.InjectDependencies(this);
        _sprite = _entMan.System<SpriteSystem>();
        _transform = _entMan.System<SharedTransformSystem>();
        _container = _entMan.System<SharedContainerSystem>();

        // Above the night vision shader, which would otherwise tint the health colors.
        ZIndex = 1;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return Allies.Count > 0 && args.Viewport.Eye == _eye.CurrentEye;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var eyeRot = args.Viewport.Eye?.Rotation ?? Angle.Zero;

        foreach (var (ally, shaders) in Allies)
        {
            if (!_entMan.TryGetComponent(ally, out SpriteComponent? sprite)
                || !_entMan.TryGetComponent(ally, out TransformComponent? xform)
                || !sprite.Visible
                || xform.MapID != args.MapId
                || _container.IsEntityOrParentInContainer(ally, xform: xform))
                continue;

            var (pos, rot) = _transform.GetWorldPositionRotation(xform);
            if (!_sprite.CalculateBounds((ally, sprite), pos, rot, eyeRot).CalcBoundingBox().Intersects(args.WorldAABB))
                continue;

            _sprite.RenderSprite((ally, sprite), handle, eyeRot, rot, pos, shaders);
        }

        handle.SetTransform(Matrix3x2.Identity);
        handle.UseShader(null);
    }
}
