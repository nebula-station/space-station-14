// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Gravity;
using Content.Shared.Gravity;

namespace Content.Server._Nebula.Gravity;

public sealed partial class MapGravitySourceSystem : EntitySystem
{
    [Dependency] private GravitySystem _gravity = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<MapGravitySourceComponent> ent, ref MapInitEvent args)
    {
        if (Transform(ent).MapUid is not { } map)
            return;

        if (HasSource(map, ent.Owner))
        {
            ent.Comp.GrantedMap = map;
            return;
        }

        var gravity = EnsureComp<GravityComponent>(map);
        if (gravity.Inherent)
            return;

        gravity.Enabled = true;
        gravity.Inherent = true;
        Dirty(map, gravity);
        ent.Comp.GrantedMap = map;

        var ev = new GravityChangedEvent(map, true);
        RaiseLocalEvent(map, ref ev, true);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<MapGravitySourceComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.GrantedMap is not { } map
            || TerminatingOrDeleted(map)
            || HasSource(map, ent.Owner)
            || !TryComp<GravityComponent>(map, out var gravity))
            return;

        gravity.Inherent = false;
        _gravity.RefreshGravity(map, gravity);
    }

    /// <summary>
    /// Whether another source already holds gravity on the map.
    /// </summary>
    private bool HasSource(EntityUid map, EntityUid except)
    {
        var query = EntityQueryEnumerator<MapGravitySourceComponent>();
        while (query.MoveNext(out var uid, out var source))
        {
            if (uid != except && source.GrantedMap == map)
                return true;
        }

        return false;
    }
}
