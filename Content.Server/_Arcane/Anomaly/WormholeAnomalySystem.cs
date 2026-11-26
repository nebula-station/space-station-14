// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Server/Anomaly/WormholeAnomalySystem.cs

using Content.Shared.Teleportation.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Arcane.Anomaly;

public sealed partial class WormholeAnomalySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private LinkedEntitySystem _linked = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<WormholeAnomalyComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextPulse = _timing.CurTime + ent.Comp.PulseInterval;
        LinkWithOtherWormholes(ent);
    }

    /// <summary>
    /// Wormholes are portals into each other (Orion #345).
    /// </summary>
    private void LinkWithOtherWormholes(EntityUid wormhole)
    {
        var query = EntityQueryEnumerator<WormholeAnomalyComponent>();
        while (query.MoveNext(out var other, out _))
        {
            if (other != wormhole)
                _linked.TryLink(wormhole, other);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<WormholeAnomalyComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (now < comp.NextPulse)
                continue;

            comp.NextPulse = now + comp.PulseInterval;

            var offset = _random.NextVector2(comp.MaxShuffleRadius);
            _transform.SetWorldPosition((uid, xform), _transform.GetWorldPosition(xform) + offset);
            _audio.PlayPvs(comp.TeleportSound, uid);
        }
    }
}
