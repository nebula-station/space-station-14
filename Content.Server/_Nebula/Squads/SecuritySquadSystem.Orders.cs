// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Shared._Nebula.Squads;
using Content.Shared.GameTicking;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Nebula.Squads;

public sealed partial class SecuritySquadSystem
{
    private static readonly TimeSpan OrderLifetime = TimeSpan.FromSeconds(40);
    private static readonly TimeSpan ExpiryCheckInterval = TimeSpan.FromSeconds(1);

    private TimeSpan _nextExpiryCheck;

    [SubscribeLocalEvent]
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _nextExpiryCheck = TimeSpan.Zero;
        _nextTabletRefresh = TimeSpan.Zero;
        _wanted.Clear();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        RefreshOpenTablets();

        if (_timing.CurTime < _nextExpiryCheck)
            return;

        _nextExpiryCheck = _timing.CurTime + ExpiryCheckInterval;
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<StationSquadsComponent>();
        while (query.MoveNext(out var uid, out var squads))
        {
            if (!squads.Orders.Exists(o => o.ExpiresAt <= now))
                continue;

            HashSet<int> expired = [];
            foreach (var order in squads.Orders)
            {
                if (order.ExpiresAt <= now)
                    expired.Add(order.Squad);
            }

            squads.Orders.RemoveAll(o => o.ExpiresAt <= now);
            foreach (var squad in squads.Squads)
            {
                if (expired.Contains(squad.Id))
                    RefreshSquad((uid, squads), squad);
            }

            UpdateTablets(uid);
        }
    }

    #region API

    /// <summary>
    /// Places the order for the squad. A squad keeps <see cref="SquadLimits.MaxOrdersPerSquad"/> orders at most,
    /// a new one pushes out the oldest.
    /// </summary>
    public bool TryPlaceOrder(EntityUid station,
        int squadId,
        ProtoId<SquadOrderPrototype> order,
        EntityCoordinates coordinates,
        [NotNullWhen(true)] out SquadOrder? placed)
    {
        placed = null;
        if (!TryComp<StationSquadsComponent>(station, out var squads)
            || squads.Squads.Find(s => s.Id == squadId) is not { } squad
            || !_proto.HasIndex(order)
            || !_gridQuery.TryComp(coordinates.EntityId, out var grid)
            || _station.GetOwningStation(coordinates.EntityId) != station
            || !SquadOrders.IsOnStationTile(_map, (coordinates.EntityId, grid), coordinates))
            return false;

        var count = 0;
        SquadOrder? oldest = null;
        foreach (var other in squads.Orders)
        {
            if (other.Squad != squadId)
                continue;

            oldest ??= other;
            count++;
        }

        if (count >= SquadLimits.MaxOrdersPerSquad && oldest != null)
            squads.Orders.Remove(oldest);

        placed = new SquadOrder
        {
            Id = squads.NextOrderId++,
            Squad = squadId,
            Order = order,
            Coordinates = coordinates,
            Location = _navMap.GetNearestBeaconString(_transform.ToMapCoordinates(coordinates), onlyName: true),
            ExpiresAt = _timing.CurTime + OrderLifetime,
        };
        squads.Orders.Add(placed);

        RefreshSquad((station, squads), squad);
        UpdateTablets(station);
        return true;
    }

    public bool TryCancelOrder(EntityUid station, int orderId, [NotNullWhen(true)] out SquadOrder? cancelled)
    {
        cancelled = null;
        if (!TryComp<StationSquadsComponent>(station, out var squads)
            || squads.Orders.Find(o => o.Id == orderId) is not { } order)
            return false;

        cancelled = order;
        squads.Orders.Remove(order);
        if (squads.Squads.Find(s => s.Id == order.Squad) is { } squad)
            RefreshSquad((station, squads), squad);

        UpdateTablets(station);
        return true;
    }

    #endregion
}
