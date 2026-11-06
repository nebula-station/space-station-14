// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.StationEvents.Components;
using Content.Shared._Nebula.Hud;
using Content.Shared.Emp;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Radio.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.Station.Components;
using Content.Shared.StationRecords.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Nebula.Hud;

/// <summary>
/// Gives the jobs of every <see cref="NeuroProfilePrototype"/> their neural interface and cuts the interfaces off
/// their servers under a radio jammer on their channel, during a solar flare, for a while after an EMP, or away from
/// the station maps for the profiles with <see cref="NeuroProfilePrototype.StationOnly"/>.
/// </summary>
public sealed partial class NeuroLinkSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    private static readonly TimeSpan SignalCheckInterval = TimeSpan.FromSeconds(0.5);
    private static readonly TimeSpan SignalLossDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan EmpSignalLoss = TimeSpan.FromSeconds(15);

    private TimeSpan _nextSignalCheck;
    private readonly Dictionary<EntityUid, TimeSpan> _jammedSince = [];
    private readonly Dictionary<EntityUid, TimeSpan> _empedUntil = [];
    private readonly HashSet<EntityUid> _jammed = [];
    private readonly HashSet<Entity<NeuroLinkComponent>> _inRange = [];
    private readonly HashSet<MapId> _stationMaps = [];

    #region Handle events

    [SubscribeLocalEvent(after: [typeof(StationRecordsSystem)])]
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (args.JobId == null
            || HasComp<NeuroLinkComponent>(args.Mob)
            || !TryGetProfile(args.JobId, out var profile))
            return;

        var link = Factory.GetComponent<NeuroLinkComponent>();
        link.Profile = profile;
        AddComp(args.Mob, link);
    }

    [SubscribeLocalEvent]
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _nextSignalCheck = TimeSpan.Zero;
        _jammedSince.Clear();
        _empedUntil.Clear();
    }

    [SubscribeLocalEvent]
    private void OnEmpPulse(Entity<NeuroLinkComponent> ent, ref EmpPulseEvent args)
    {
        var now = _timing.CurTime;
        var until = now + EmpSignalLoss;
        if (!_empedUntil.TryGetValue(ent, out var current) || current < until)
            _empedUntil[ent] = until;

        _jammedSince.TryAdd(ent, now);
        EnsureComp<NeuroSignalLostComponent>(ent);
        args.Affected = true;
    }

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextSignalCheck)
            return;

        _nextSignalCheck = _timing.CurTime + SignalCheckInterval;
        CollectJammed();
        CollectEmped();
        if (_jammed.Count == 0 && _jammedSince.Count == 0)
            return;

        foreach (var uid in _jammed)
        {
            if (!_jammedSince.TryAdd(uid, _timing.CurTime)
                && _timing.CurTime - _jammedSince[uid] >= SignalLossDelay)
                EnsureComp<NeuroSignalLostComponent>(uid);
        }

        List<EntityUid> restored = [];
        foreach (var uid in _jammedSince.Keys)
        {
            if (!_jammed.Contains(uid))
                restored.Add(uid);
        }

        foreach (var uid in restored)
        {
            _jammedSince.Remove(uid);
            if (!RemComp<NeuroSignalLostComponent>(uid))
                continue;

            var ev = new NeuroSignalRestoredEvent();
            RaiseLocalEvent(uid, ref ev);
        }
    }

    #region Helpers

    private bool TryGetProfile(string job, out ProtoId<NeuroProfilePrototype> profile)
    {
        foreach (var proto in _proto.EnumeratePrototypes<NeuroProfilePrototype>())
        {
            var matches = proto.Jobs.Contains(job);
            foreach (var department in proto.Departments)
            {
                matches |= _proto.Index(department).Roles.Contains(job);
            }

            if (!matches)
                continue;

            profile = proto.ID;
            return true;
        }

        profile = default;
        return false;
    }

    private void CollectEmped()
    {
        if (_empedUntil.Count == 0)
            return;

        List<EntityUid> expired = [];
        foreach (var (uid, until) in _empedUntil)
        {
            if (_timing.CurTime >= until || TerminatingOrDeleted(uid))
                expired.Add(uid);
            else
                _jammed.Add(uid);
        }

        foreach (var uid in expired)
        {
            _empedUntil.Remove(uid);
        }
    }

    private void CollectJammed()
    {
        _jammed.Clear();
        if (IsFlareActive())
        {
            var all = EntityQueryEnumerator<NeuroLinkComponent>();
            while (all.MoveNext(out var uid, out _))
            {
                _jammed.Add(uid);
            }

            return;
        }

        CollectOffStation();
        var query = EntityQueryEnumerator<ActiveRadioJammerComponent, RadioJammerComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var jammer, out var xform))
        {
            _inRange.Clear();
            _lookup.GetEntitiesInRange(xform.Coordinates, jammer.Settings[jammer.SelectedPowerLevel].Range, _inRange);
            foreach (var link in _inRange)
            {
                if (!_proto.Resolve(link.Comp.Profile, out var profile)
                    || jammer.FrequenciesExcluded.Contains(_proto.Index(profile.Channel).Frequency))
                    continue;

                _jammed.Add(link.Owner);
            }
        }
    }

    /// <summary>
    /// A map with a station grid counts as the station, so a walk outside the hull keeps the link.
    /// The arrivals shuttle counts too, late joiners spawn on it away from the station map.
    /// </summary>
    private void CollectOffStation()
    {
        _stationMaps.Clear();
        var grids = EntityQueryEnumerator<StationMemberComponent, TransformComponent>();
        while (grids.MoveNext(out _, out var xform))
        {
            _stationMaps.Add(xform.MapID);
        }

        var links = EntityQueryEnumerator<NeuroLinkComponent, TransformComponent>();
        while (links.MoveNext(out var uid, out var link, out var xform))
        {
            if (_stationMaps.Contains(xform.MapID)
                || HasComp<ArrivalsShuttleComponent>(xform.GridUid)
                || !_proto.Resolve(link.Profile, out var profile)
                || !profile.StationOnly)
                continue;

            _jammed.Add(uid);
        }
    }

    private bool IsFlareActive()
    {
        var query = EntityQueryEnumerator<SolarFlareRuleComponent, ActiveGameRuleComponent>();
        return query.MoveNext(out _, out _);
    }

    #endregion
}
