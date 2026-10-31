// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.DeviceLinking.Components;
using Content.Server.Medical.CrewMonitoring;
using Content.Shared._Nebula.Hud;
using Content.Shared._Nebula.Squads;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.SuitSensors;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;

namespace Content.Server._Nebula.Squads;

public sealed partial class SecuritySquadSystem
{
    private static readonly EntProtoId BrigTimer = "BrigTimer";
    private static readonly TimeSpan CombatTime = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan CombatWindow = TimeSpan.FromSeconds(2);
    private static readonly FixedPoint2 CombatDamage = 20;
    private static readonly TimeSpan TabletRefreshInterval = TimeSpan.FromSeconds(0.5);
    private const int MaxNewMembers = 32;

    private TimeSpan _nextTabletRefresh;

    #region Component events

    [SubscribeLocalEvent]
    private void OnTabletOpened(Entity<SquadTabletComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateTablet(ent);
    }

    #endregion

    #region BUI events

    [SubscribeLocalEvent]
    private void HandleCreateSquadMessage(Entity<SquadTabletComponent> ent, ref SquadTabletCreateSquadMessage args)
    {
        if (!TryGetActorStation(ent, args.Actor, out var station))
            return;

        if (args.Color < 0 || args.Color >= SquadPalette.Colors.Length)
        {
            LogInvalid(args.Actor, "squad color");
            return;
        }

        if (args.Name == null
            || args.Members == null
            || args.Members.Count > MaxNewMembers)
        {
            LogInvalid(args.Actor, "squad data");
            return;
        }

        if (!TryCreateSquad(station, args.Name, SquadPalette.Colors[args.Color], out var squad))
            return;

        Entity<StationSquadsComponent> squads = (station, Comp<StationSquadsComponent>(station));
        HashSet<SecuritySquad> touched = [];
        foreach (var record in args.Members)
        {
            var key = new StationRecordKey(record, station);
            if (_records.TryGetRecord<GeneralStationRecord>(key, out var general)
                && IsSquadCandidate(general.JobPrototype))
                MoveMember(squads, key, squad, touched);
        }

        RefreshTouched(squads, touched);

        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.Actor):user} created security squad {squad.Name} on {ToPrettyString(station):station}");
    }

    [SubscribeLocalEvent]
    private void HandleDeleteSquadMessage(Entity<SquadTabletComponent> ent, ref SquadTabletDeleteSquadMessage args)
    {
        var id = args.Squad;
        if (!TryGetActorStation(ent, args.Actor, out var station)
            || !TryComp<StationSquadsComponent>(station, out var squads)
            || squads.Squads.Find(s => s.Id == id) is not { } squad)
            return;

        DeleteSquad((station, squads), squad.Id);
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.Actor):user} disbanded security squad {squad.Name} on {ToPrettyString(station):station}");
    }

    [SubscribeLocalEvent]
    private void HandleAssignMessage(Entity<SquadTabletComponent> ent, ref SquadTabletAssignMessage args)
    {
        if (!TryGetActorStation(ent, args.Actor, out var station))
            return;

        var key = new StationRecordKey(args.Record, station);
        if (!_records.TryGetRecord<GeneralStationRecord>(key, out var record)
            || !IsSquadCandidate(record.JobPrototype))
        {
            LogInvalid(args.Actor, "squad member");
            return;
        }

        if (!TrySetSquad(key, args.Squad))
            return;

        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.Actor):user} moved {record.Name} to security squad {args.Squad?.ToString() ?? "none"}");
    }

    [SubscribeLocalEvent]
    private void HandlePlaceOrderMessage(Entity<SquadTabletComponent> ent, ref SquadTabletPlaceOrderMessage args)
    {
        if (!TryGetActorStation(ent, args.Actor, out var station)
            || !TryPlaceOrder(station, args.Squad, args.Order, GetCoordinates(args.Coordinates), out var order))
            return;

        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.Actor):user} gave order {order.Order} at {order.Location} to security squad {order.Squad}");
    }

    [SubscribeLocalEvent]
    private void HandleCancelOrderMessage(Entity<SquadTabletComponent> ent, ref SquadTabletCancelOrderMessage args)
    {
        if (!TryGetActorStation(ent, args.Actor, out var station)
            || !TryCancelOrder(station, args.Order, out var order))
            return;

        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.Actor):user} cancelled order {order.Order} of security squad {order.Squad}");
    }

    [SubscribeLocalEvent]
    private void HandleCreateTaskMessage(Entity<SquadTabletComponent> ent, ref SquadTabletCreateTaskMessage args)
    {
        if (!TryGetActorStation(ent, args.Actor, out var station))
            return;

        if (args.Title == null || args.Description == null)
        {
            LogInvalid(args.Actor, "squad task");
            return;
        }

        StationRecordKey? member = args.Record is { } record ? new StationRecordKey(record, station) : null;
        if (!TryCreateTask(station, args.Squad, member, args.Title, args.Description, out var task))
            return;

        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.Actor):user} gave security task \"{task.Title}\" to squad {task.Squad?.ToString() ?? "-"}, record {task.Member?.Id.ToString() ?? "-"}");
    }

    [SubscribeLocalEvent]
    private void HandleDeleteTaskMessage(Entity<SquadTabletComponent> ent, ref SquadTabletDeleteTaskMessage args)
    {
        if (!TryGetActorStation(ent, args.Actor, out var station)
            || !TryDeleteTask(station, args.Task, out var task))
            return;

        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.Actor):user} removed security task \"{task.Title}\"");
    }

    #endregion

    #region Validation

    private bool TryGetActorStation(Entity<SquadTabletComponent> ent, EntityUid actor, out EntityUid station)
    {
        station = default;
        if (!_actionBlocker.CanInteract(actor, ent.Owner)
            || !_access.IsAllowed(actor, ent.Owner)
            || GetTabletStation(ent) is not { } found)
            return false;

        station = found;
        return true;
    }

    private void LogInvalid(EntityUid actor, string what)
    {
        _adminLog.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(actor):user} sent invalid {what} to the squad tablet (possible modified client)");
    }

    #endregion

    #region Helpers

    /// <summary>
    /// The station the tablet is on, or the one it was last used on.
    /// </summary>
    private EntityUid? GetTabletStation(Entity<SquadTabletComponent> ent)
    {
        if (_station.GetOwningStation(ent.Owner) is { } station)
            ent.Comp.Station = station;

        return ent.Comp.Station;
    }

    /// <summary>
    /// Sensor broadcasts come every few seconds, so open tablets are refreshed on their own
    /// to keep member positions live.
    /// </summary>
    private void RefreshOpenTablets()
    {
        if (_timing.CurTime < _nextTabletRefresh)
            return;

        _nextTabletRefresh = _timing.CurTime + TabletRefreshInterval;
        var query = EntityQueryEnumerator<SquadTabletComponent>();
        while (query.MoveNext(out var uid, out var tablet))
        {
            UpdateTablet((uid, tablet));
        }
    }

    private void UpdateTablets(EntityUid station)
    {
        var query = EntityQueryEnumerator<SquadTabletComponent>();
        while (query.MoveNext(out var uid, out var tablet))
        {
            if (tablet.Station == station || _station.GetOwningStation(uid) == station)
                UpdateTablet((uid, tablet));
        }
    }

    private void UpdateTablet(Entity<SquadTabletComponent> ent)
    {
        if (!_ui.IsUiOpen(ent.Owner, SquadTabletUiKey.Key)
            || GetTabletStation(ent) is not { } station)
            return;

        Dictionary<NetEntity, SuitSensorStatus> sensors = [];
        if (TryComp<CrewMonitoringConsoleComponent>(ent, out var console))
        {
            foreach (var sensor in console.ConnectedSensors.Values)
            {
                sensors[sensor.OwnerUid] = sensor;
            }
        }

        Dictionary<uint, (EntityUid Uid, bool InCombat)> mobs = [];
        var query = EntityQueryEnumerator<SecurityNeuroComponent>();
        while (query.MoveNext(out var uid, out var neuro))
        {
            if (neuro.Record is { } key && key.OriginStation == station)
                mobs[key.Id] = (uid, neuro.LastHurt + CombatTime > _timing.CurTime);
        }

        List<SquadTabletSquad> squadStates = [];
        List<SquadTabletOrder> orderStates = [];
        List<SquadTabletTask> taskStates = [];
        HashSet<uint> taken = [];
        if (TryComp<StationSquadsComponent>(station, out var squads))
        {
            foreach (var squad in squads.Squads)
            {
                List<SquadTabletMember> members = [];
                foreach (var key in squad.Members)
                {
                    taken.Add(key.Id);
                    if (_records.TryGetRecord<GeneralStationRecord>(key, out var record))
                        members.Add(BuildMember(key.Id, record, mobs, sensors));
                }

                squadStates.Add(new SquadTabletSquad { Id = squad.Id, Name = squad.Name, Color = squad.Color, Members = members });
            }

            foreach (var order in squads.Orders)
            {
                orderStates.Add(new SquadTabletOrder
                {
                    Id = order.Id,
                    Squad = order.Squad,
                    Order = order.Order,
                    Coordinates = GetNetCoordinates(order.Coordinates),
                });
            }

            foreach (var task in squads.Tasks)
            {
                taskStates.Add(new SquadTabletTask
                {
                    Id = task.Id,
                    Squad = task.Squad,
                    Record = task.Member?.Id,
                    Title = task.Title,
                    Description = task.Description,
                });
            }
        }

        List<SquadTabletMember> free = [];
        foreach (var (id, record) in _records.GetRecordsOfType<GeneralStationRecord>(station))
        {
            if (!taken.Contains(id) && IsSquadCandidate(record.JobPrototype))
                free.Add(BuildMember(id, record, mobs, sensors));
        }

        _ui.SetUiState(ent.Owner,
            SquadTabletUiKey.Key,
            new SquadTabletState(squadStates, free, orderStates, taskStates, GetTimers(station)));
    }

    /// <summary>
    /// Running brig timers on the station, named by their label or the nearest beacon.
    /// </summary>
    private List<SquadTabletTimer> GetTimers(EntityUid station)
    {
        List<SquadTabletTimer> timers = [];
        var query = EntityQueryEnumerator<ActiveSignalTimerComponent, SignalTimerComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var active, out var timer, out var meta))
        {
            if (meta.EntityPrototype?.ID != BrigTimer.Id
                || _station.GetOwningStation(uid) != station)
                continue;

            var name = timer.Label.Length > 0
                ? timer.Label
                : _navMap.GetNearestBeaconString(uid, onlyName: true);
            timers.Add(new SquadTabletTimer { Entity = GetNetEntity(uid), Name = name, End = active.TriggerTime });
        }

        return timers;
    }

    private SquadTabletMember BuildMember(uint id,
        GeneralStationRecord record,
        Dictionary<uint, (EntityUid Uid, bool InCombat)> mobs,
        Dictionary<NetEntity, SuitSensorStatus> sensors)
    {
        var member = new SquadTabletMember { Record = id, Name = record.Name, Job = record.JobTitle };
        if (!mobs.TryGetValue(id, out var mob))
            return member;

        var entity = GetNetEntity(mob.Uid);
        member.Entity = entity;
        member.InCombat = mob.InCombat;
        if (!sensors.TryGetValue(entity, out var sensor))
            return member;

        member.Signal = sensor.IsAlive ? SquadMemberSignal.Alive : SquadMemberSignal.Dead;

        if (sensor.Coordinates != null)
            member.Coordinates = GetNetCoordinates(_transform.GetMoverCoordinates(mob.Uid));
        return member;
    }

    #endregion
}
