using System.Numerics;
using Content.Client.Animations;
using Content.Client.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Map;

namespace Content.Client.Weapons.Melee;

public sealed partial class MeleeWeaponSystem
{
    private const string FadeAnimationKey = "melee-fade";
    private const string SlashAnimationKey = "melee-slash";
    private const string ThrustAnimationKey = "melee-thrust";

    // Nebula - bounds for the attack rate based animation length
    private const float DefaultAnimationLength = 0.6f;
    private const float MinAnimationLength = 0.15f;
    private const float MaxAnimationLength = 1f;

    /// <summary>
    /// Does all of the melee effects for a player that are predicted, i.e. character lunge and weapon animation.
    /// </summary>
    public override void DoLunge(EntityUid user, EntityUid weapon, Angle angle, Vector2 localPos, string? animation, Angle spriteRotation, bool flippedAnimation, bool predicted = true, EntityUid? source = null) // Trauma - source
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        // <Trauma>
        if (source != null && source != user)
        {
            user = source.Value;
            weapon = user;
        }
        // </Trauma>

        var lunge = GetLungeAnimation(localPos);

        // Stop any existing lunges on the user.
        _animation.Stop(user, MeleeLungeKey);
        _animation.Play(user, lunge, MeleeLungeKey);

        if (localPos == Vector2.Zero || animation == null)
            return;

        if (!TryComp(user, out TransformComponent? userXform) || userXform.MapID == MapId.Nullspace)
            return;

        var animationUid = Spawn(animation, userXform.Coordinates);

        if (!TryComp<SpriteComponent>(animationUid, out var sprite)
            || !TryComp<WeaponArcVisualsComponent>(animationUid, out var arcComponent))
        {
            return;
        }

        // Nebula - Monolith melee animations: duration follows the attack rate, swing reach follows AnimationOffset
        var length = DefaultAnimationLength;
        var offset = 1f;

        if (arcComponent.Animation != WeaponArcAnimation.None
            && TryComp(weapon, out MeleeWeaponComponent? meleeWeaponComponent))
        {
            if (user != weapon
                && TryComp(weapon, out SpriteComponent? weaponSpriteComponent))
                _sprite.CopySprite((weapon, weaponSpriteComponent), (animationUid, sprite));

            if (meleeWeaponComponent.SwingLeft)
                angle *= -1;

            length = Math.Clamp(1f / MathF.Max(meleeWeaponComponent.AttackRate, 0.01f) * 0.6f, MinAnimationLength, MaxAnimationLength);
            offset = meleeWeaponComponent.AnimationOffset;
        }
        _sprite.SetRotation((animationUid, sprite), localPos.ToWorldAngle());

        var xform = Transform(animationUid);
        TrackUserComponent track;

        switch (arcComponent.Animation)
        {
            case WeaponArcAnimation.Slash:
                track = EnsureComp<TrackUserComponent>(animationUid);
                track.User = user;
                _animation.Play(animationUid, GetSlashAnimation(sprite, angle, spriteRotation, length, offset), SlashAnimationKey);
                if (arcComponent.Fadeout)
                    _animation.Play(animationUid, GetFadeAnimation(sprite, length * 0.5f, length + 0.15f), FadeAnimationKey);
                break;
            case WeaponArcAnimation.Thrust:
                track = EnsureComp<TrackUserComponent>(animationUid);
                track.User = user;
                // Goobstation - Shove Rework Start
                var dir = Transform(user).LocalRotation.GetCardinalDir();

                if (flippedAnimation && dir is Direction.South or Direction.West)
                    spriteRotation = Angle.FromDegrees(360) - spriteRotation;

                // Goobstation - Shove Rework End
                _animation.Play(animationUid, GetThrustAnimation((animationUid, sprite), offset, spriteRotation, length), ThrustAnimationKey);
                if (arcComponent.Fadeout)
                    _animation.Play(animationUid, GetFadeAnimation(sprite, length * 0.5f, length + 0.15f), FadeAnimationKey);
                break;
            case WeaponArcAnimation.None:
                var (mapPos, mapRot) = TransformSystem.GetWorldPositionRotation(userXform);
                var worldPos = mapPos + (mapRot - userXform.LocalRotation).RotateVec(localPos);
                var newLocalPos = Vector2.Transform(worldPos, TransformSystem.GetInvWorldMatrix(xform.ParentUid));
                TransformSystem.SetLocalPositionNoLerp(animationUid, newLocalPos, xform);
                if (arcComponent.Fadeout)
                    _animation.Play(animationUid, GetFadeAnimation(sprite, 0f, 0.15f), FadeAnimationKey);
                break;
        }
    }

    // Nebula - Monolith slash: a quick swing past the target and a slow settle back
    private Animation GetSlashAnimation(SpriteComponent sprite, Angle arc, Angle spriteRotation, float length, float offset)
    {
        var startRotation = sprite.Rotation + arc * 0.5f;
        var endRotation = sprite.Rotation - arc * 0.5f;

        var startRotationOffset = startRotation.RotateVec(new Vector2(0f, -offset * 0.9f));
        var minRotationOffset = sprite.Rotation.RotateVec(new Vector2(0f, -offset * 1.1f));
        var endRotationOffset = endRotation.RotateVec(new Vector2(0f, -offset * 0.9f));

        startRotation += spriteRotation;
        endRotation += spriteRotation;

        return new Animation()
        {
            Length = TimeSpan.FromSeconds(length + 0.05f),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(startRotation, 0f),
                        new AnimationTrackProperty.KeyFrame(Angle.Lerp(startRotation, endRotation, 0.5f), length * 0.10f),
                        new AnimationTrackProperty.KeyFrame(endRotation, length * 0.15f),
                        new AnimationTrackProperty.KeyFrame(Angle.Lerp(startRotation, endRotation, 0.9f), length * 0.20f),
                        new AnimationTrackProperty.KeyFrame(Angle.Lerp(startRotation, endRotation, 0.8f), length * 0.6f, Easings.OutQuart),
                    },
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(startRotationOffset, 0f),
                        new AnimationTrackProperty.KeyFrame(minRotationOffset, length * 0.10f),
                        new AnimationTrackProperty.KeyFrame(endRotationOffset, length * 0.15f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Lerp(startRotationOffset, endRotationOffset, 0.8f), length * 0.6f, Easings.OutQuart),
                    },
                },
            },
        };
    }

    // Nebula - Monolith thrust: a stab with a small recoil
    private Animation GetThrustAnimation(Entity<SpriteComponent> sprite, float offset, Angle spriteRotation, float length)
    {
        var startOffset = Vector2.Zero;
        var endOffset = sprite.Comp.Rotation.RotateVec(new Vector2(0f, -offset * 1.2f));
        _sprite.SetRotation(sprite.AsNullable(), sprite.Comp.Rotation + spriteRotation);

        return new Animation()
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(startOffset, 0f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Lerp(startOffset, endOffset, 0.65f), length * 0.10f),
                        new AnimationTrackProperty.KeyFrame(endOffset, length * 0.20f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Lerp(startOffset, endOffset, 0.9f), length * 0.30f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Lerp(startOffset, endOffset, 0.7f), length * 0.60f, Easings.OutQuart),
                    },
                },
            },
        };
    }

    private Animation GetFadeAnimation(SpriteComponent sprite, float start, float end)
    {
        return new Animation
        {
            Length = TimeSpan.FromSeconds(end),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Color, start),
                        new AnimationTrackProperty.KeyFrame(sprite.Color.WithAlpha(0f), end)
                    }
                }
            }
        };
    }

    /// <summary>
    /// Get the sprite offset animation to use for mob lunges.
    /// </summary>
    private Animation GetLungeAnimation(Vector2 direction)
    {
        const float length = 0.1f;

        return new Animation
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        // Nebula - Monolith lunge: lean into the hit instead of snapping forward
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0f),
                        new AnimationTrackProperty.KeyFrame(direction.Normalized() * 0.15f, length * 0.4f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, length * 0.6f),
                    }
                }
            }
        };
    }

    /// <summary>
    /// Updates the effect positions to follow the user
    /// </summary>
    private void UpdateEffects()
    {
        var query = EntityQueryEnumerator<TrackUserComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var arcComponent, out var xform))
        {
            if (arcComponent.User == null || TerminatingOrDeleted(arcComponent.User) || !xform.ParentUid.IsValid()) // Goobstation, Nebula - parent check
                continue;

            Vector2 targetPos = TransformSystem.GetWorldPosition(arcComponent.User.Value);

            if (arcComponent.Offset != Vector2.Zero)
            {
                var entRotation = TransformSystem.GetWorldRotation(xform);
                targetPos += entRotation.RotateVec(arcComponent.Offset);
            }

            // Nebula - Monolith: local position without lerp, so the arc does not trail behind a moving user
            var localPos = Vector2.Transform(targetPos, TransformSystem.GetInvWorldMatrix(xform.ParentUid));
            TransformSystem.SetLocalPositionNoLerp(uid, localPos, xform);
        }
    }

    private Angle? GetParentRotation(TransformComponent xform) // Goobstation
    {
        if (xform.ParentUid == xform.GridUid || xform.ParentUid == xform.MapUid || !Exists(xform.ParentUid))
            return TransformSystem.GetWorldRotation(xform);

        return null;
    }
}
