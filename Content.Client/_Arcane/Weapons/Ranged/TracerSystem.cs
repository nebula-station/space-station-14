// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Client/Weapons/Ranged/TracerSystem.cs

using System.Numerics;
using Content.Shared._Arcane.Weapons.Ranged;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client._Arcane.Weapons.Ranged;

public sealed partial class TracerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private TracerOverlay _tracerOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _tracerOverlay = new TracerOverlay(this);
        _overlay.AddOverlay(_tracerOverlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay(_tracerOverlay);
    }

    [SubscribeLocalEvent]
    private void OnTracerStartup(Entity<TracerComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.PositionHistory.Clear();
        ent.Comp.PositionHistory.Add(_transform.GetWorldPosition(ent));
        ent.Comp.EndTime = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.Lifetime);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<TracerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var tracer, out var xform))
        {
            if (curTime > tracer.EndTime)
            {
                RemCompDeferred<TracerComponent>(uid);
                continue;
            }

            // A projectile that did not move adds nothing to the trail.
            var positions = tracer.PositionHistory;
            var position = _transform.GetWorldPosition(xform);
            if (positions.Count > 0 && positions[^1] == position)
                continue;

            if (positions.Count > 0)
                tracer.TrailLength += Vector2.Distance(positions[^1], position);
            positions.Add(position);

            while (positions.Count > 2 && tracer.TrailLength > tracer.Length)
            {
                tracer.TrailLength -= Vector2.Distance(positions[0], positions[1]);
                positions.RemoveAt(0);
            }
        }
    }

    public void Draw(DrawingHandleWorld handle, MapId currentMap)
    {
        handle.SetTransform(Matrix3x2.Identity);

        var query = EntityQueryEnumerator<TracerComponent, TransformComponent>();
        while (query.MoveNext(out _, out var tracer, out var xform))
        {
            if (xform.MapID != currentMap)
                continue;

            var positions = tracer.PositionHistory;
            for (var i = 1; i < positions.Count; i++)
            {
                handle.DrawLine(positions[i - 1], positions[i], tracer.Color);
            }
        }
    }
}
