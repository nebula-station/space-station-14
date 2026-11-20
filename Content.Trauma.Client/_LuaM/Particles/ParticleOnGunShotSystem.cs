// Ported from Monolith-DS (LuaM, from Starfall/Hullrot) by Nebula.

using Content.Shared._LuaM.Weapons;
using Content.Trauma.Client.Particles;
using Content.Trauma.Shared._LuaM.Particles;
using Content.Trauma.Shared.Particles;
using Robust.Client.Graphics;
using Robust.Shared.Containers;

namespace Content.Trauma.Client._LuaM.Particles;

/// <summary>
/// Spawns the <see cref="ParticleOnGunShotComponent"/> effect when the gun shows its muzzle flash.
/// Monolith listened to AmmoShotEvent, which is only raised on the server, the muzzle flash is shown
/// on every client for own and other players' shots.
/// </summary>
public sealed partial class ParticleOnGunShotSystem : EntitySystem
{
    [Dependency] private ParticleSystem _particles = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private IEyeManager _eye = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParticleOnGunShotComponent, GunMuzzleFlashedEvent>(OnGunShot);
    }

    private void OnGunShot(Entity<ParticleOnGunShotComponent> ent, ref GunMuzzleFlashedEvent args)
    {
        if (!ProtoMan.TryIndex(ent.Comp.Effect, out var proto))
        {
            Log.Error($"{ToPrettyString(ent)} references unknown particle effect '{ent.Comp.Effect}'");
            return;
        }

        if (proto.Duration == TimeSpan.Zero)
        {
            Log.Warning($"{ToPrettyString(ent)} tried to spawn infinite-duration effect '{ent.Comp.Effect}' on a shot, it would never stop emitting.");
            return;
        }

        // The smoke follows whoever holds the gun.
        var target = _container.TryGetContainingContainer(ent.Owner, out var container)
            ? container.Owner
            : ent.Owner;

        ParticleRuntimeOverrides? overrides = null;
        if (ent.Comp.InheritAngle)
        {
            // The shot angle is in map space, particles emit in screen space, so the eye rotation is added.
            // Without it the smoke is turned by the grid rotation on rotated shuttles and stations.
            var angle = args.Angle + _eye.CurrentEye.Rotation;
            overrides = new ParticleRuntimeOverrides { EmitAngle = -angle + Angle.FromDegrees(90) };
        }

        _particles.CreateParticle(ent.Comp.Effect, target, ent.Comp.ColorOverride, overrides: overrides);
    }
}
