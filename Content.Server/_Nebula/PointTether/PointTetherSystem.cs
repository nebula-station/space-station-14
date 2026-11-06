// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.PointTether;
using Robust.Shared.Timing;

namespace Content.Server._Nebula.PointTether;

public sealed partial class PointTetherSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<PointTetherComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var tether, out var xform))
        {
            if (now < tether.NextCheck)
                continue;

            tether.NextCheck = now + tether.CheckInterval;

            if (TerminatingOrDeleted(tether.Anchor))
                continue;

            var anchorCoords = _transform.GetMapCoordinates(tether.Anchor);
            var coords = _transform.GetMapCoordinates(uid, xform);
            if (coords.MapId == anchorCoords.MapId && coords.InRange(anchorCoords, tether.Range))
                continue;

            var ev = new PointTetherExceededEvent();
            RaiseLocalEvent(uid, ref ev);
        }
    }

    public void SetTether(EntityUid uid, EntityUid anchor, float range, TimeSpan checkInterval)
    {
        var tether = EnsureComp<PointTetherComponent>(uid);
        tether.Anchor = anchor;
        tether.Range = range;
        tether.CheckInterval = checkInterval;
        tether.NextCheck = _timing.CurTime + checkInterval;
        Dirty(uid, tether);
    }
}
