// Ported from Monolith-DS (LuaM, from Starfall/Hullrot) by Nebula.

using Content.Trauma.Shared.Particles;

namespace Content.Trauma.Shared._LuaM.Particles;

/// <summary>
/// Spawns a particle effect at the gun on every shot, e.g. powder smoke out of the barrel.
/// The effect must have a finite duration, it is not stopped by anything.
/// </summary>
[RegisterComponent]
public sealed partial class ParticleOnGunShotComponent : Component
{
    [DataField(required: true)]
    public ProtoId<ParticleEffectPrototype> Effect;

    /// <summary>
    /// Optional color tint applied to the spawned particles.
    /// </summary>
    [DataField]
    public Color? ColorOverride;

    /// <summary>
    /// Emit the particles along the shot direction instead of the effect's own emit angle.
    /// </summary>
    [DataField]
    public bool InheritAngle;
}
