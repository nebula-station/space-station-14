// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Particles;

namespace Content.Trauma.Client.Particles;

/// <summary>
/// Spawns a particle effect on this client when an entity with
/// <see cref="ParticleEmitterComponent"/> enters the local view (ComponentStartup).
/// </summary>
public sealed partial class ParticleEmitterSystem : EntitySystem
{
    [Dependency] private ParticleSystem _particles = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Nebula - Monolith: MapInitEvent -> ComponentStartup, the client never raises map init for entities
        // created from server state, so emitters spawned by the server (explosion visuals) never started.
        SubscribeLocalEvent<ParticleEmitterComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<ParticleEmitterComponent> ent, ref ComponentStartup args) // Nebula - Monolith
    {
        var coords = _transform.GetMapCoordinates(ent.Owner);
        var emitter = _particles.SpawnEffect(ent.Comp.Effect, coords, ent.Owner, ent.Comp.ColorOverride);
        if (emitter == null)
            return;

        if (ent.Comp.Intensity != 1f)
            emitter.Intensity = ent.Comp.Intensity;
    }
}
