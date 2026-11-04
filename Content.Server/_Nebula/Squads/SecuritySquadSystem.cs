// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._Nebula.Hud;
using Content.Server.Administration.Logs;
using Content.Server.Inventory;
using Content.Server.Pinpointer;
using Content.Shared._Nebula.Hud;
using Content.Shared._Nebula.Squads;
using Content.Shared.Access.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles;
using Content.Shared.Station.Systems;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Events;
using Content.Shared.StationRecords.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Nebula.Squads;

/// <summary>
/// Gives every security member the neural interface and keeps the squad data in their interfaces up to date.
/// Squads, their orders and tasks live on the station in <see cref="StationSquadsComponent"/>,
/// the head of security changes them from the squad tablet.
/// </summary>
public sealed partial class SecuritySquadSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private StationRecordsSystem _records = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private ServerInventorySystem _inventory = default!;

    [Dependency] private EntityQuery<StationRecordKeyStorageComponent> _keyStorageQuery = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    private static readonly ProtoId<DepartmentPrototype> SecurityDepartment = "Security";
    private static readonly ProtoId<NeuroProfilePrototype> SecurityProfile = "Security";
    private static readonly ProtoId<JobPrototype> HeadOfSecurity = "HeadOfSecurity";

    #region Component events

    [SubscribeLocalEvent(after: [typeof(StationRecordsSystem), typeof(NeuroLinkSystem)])]
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!TryComp<NeuroLinkComponent>(args.Mob, out var link)
            || link.Profile != SecurityProfile)
            return;

        var neuro = EnsureComp<SecurityNeuroComponent>(args.Mob);
        neuro.Commander = args.JobId is { } job && job == HeadOfSecurity;
        DirtyField(args.Mob, neuro, nameof(SecurityNeuroComponent.Commander));
        if (!_idCard.TryFindIdCard(args.Mob, out var card)
            || !_keyStorageQuery.TryComp(card, out var keyStorage)
            || keyStorage.Key is not { } key)
            return;

        neuro.Record = key;
        if (TryGetSquad(key, out var squads, out var squad))
            RefreshSquad(squads, squad);

        RefreshTasks((args.Mob, neuro));
        if (TryComp<StationSquadsComponent>(key.OriginStation, out var stationSquads))
            RefreshSummary((key.OriginStation, stationSquads));
    }

    /// <summary>
    /// The squad data and the tasks froze while the link was down, so they are sent again right away.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnSignalRestored(Entity<SecurityNeuroComponent> ent, ref NeuroSignalRestoredEvent args)
    {
        RefreshTasks(ent);
        if (ent.Comp.Record is not { } key
            || !TryComp<StationSquadsComponent>(key.OriginStation, out var squads))
            return;

        if (TryGetSquad(key, out var squad))
            RefreshSquad((key.OriginStation, squads), squad);
        else
            RefreshSummary((key.OriginStation, squads));
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<SecurityNeuroComponent> ent, ref MobStateChangedEvent args)
    {
        if (ent.Comp.Record is not { } key
            || !TryGetSquad(key, out var squads, out var squad))
            return;

        RefreshSquad(squads, squad);
        if (args.NewMobState == MobState.Dead
            || args.NewMobState == MobState.Critical && args.OldMobState != MobState.Dead)
            AlertCommanders(squads, squad, key, args.NewMobState);
    }

    /// <summary>
    /// The member counts as in combat once the damage within <see cref="CombatWindow"/> goes over <see cref="CombatDamage"/>,
    /// the one who dealt it gets marked on the tablet map.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnDamageDealt(Entity<SecurityNeuroComponent> ent, ref DamageDealtEvent args)
    {
        var damage = FixedPoint2.Zero;
        foreach (var value in args.Damage.DamageDict.Values)
        {
            if (value > 0)
                damage += value;
        }

        if (damage == FixedPoint2.Zero)
            return;

        var now = _timing.CurTime;
        var recent = ent.Comp.RecentDamage;
        var expired = 0;
        while (expired < recent.Count && recent[expired].At + CombatWindow < now)
        {
            expired++;
        }

        recent.RemoveRange(0, expired);
        recent.Add((now, damage));

        var total = FixedPoint2.Zero;
        foreach (var (_, amount) in recent)
        {
            total += amount;
        }

        if (total <= CombatDamage)
            return;

        ent.Comp.LastHurt = now;
        if (args.Origin is { } attacker
            && attacker != ent.Owner
            && _mobStateQuery.HasComp(attacker)
            && !HasComp<SecurityNeuroComponent>(attacker))
        {
            ent.Comp.LastAttacker = attacker;
            ent.Comp.LastAttackerAt = now;
        }
    }

    /// <summary>
    /// Dropping the record first makes <see cref="RefreshSquad"/> treat the member as bodiless.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnShutdown(Entity<SecurityNeuroComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Record is not { } key)
            return;

        ent.Comp.Record = null;
        if (TryGetSquad(key, out var squads, out var squad))
            RefreshSquad(squads, squad);
    }

    [SubscribeLocalEvent]
    private void OnRecordRemoved(ref RecordRemovedEvent args)
    {
        var key = args.Key;
        TrySetSquad(key, null);
        if (TryComp<StationSquadsComponent>(key.OriginStation, out var squads))
            squads.Tasks.RemoveAll(t => t.Member is { } member && member.Equals(key));

        // The record is gone already, so this also drops the wanted task if it was one.
        RefreshAllTasks(key.OriginStation);
        UpdateTablets(key.OriginStation);
    }

    #endregion

    #region API

    public bool TryCreateSquad(EntityUid station, string name, Color color, [NotNullWhen(true)] out SecuritySquad? squad)
    {
        squad = null;
        name = name.Trim();
        var squads = EnsureComp<StationSquadsComponent>(station);
        if (squads.Squads.Count >= SquadLimits.MaxSquads
            || name.Length == 0
            || name.Length > SquadLimits.MaxNameLength
            || squads.Squads.Exists(s => s.Name == name || s.Color == color))
            return false;

        squad = new SecuritySquad { Id = squads.NextId++, Name = name, Color = color };
        squads.Squads.Add(squad);
        RefreshSummary((station, squads));
        UpdateTablets(station);
        return true;
    }

    /// <summary>
    /// Disbands the squad, its members become free, its orders and tasks are dropped.
    /// </summary>
    public void DeleteSquad(Entity<StationSquadsComponent?> station, int id)
    {
        if (!Resolve(station, ref station.Comp, false)
            || station.Comp.Squads.Find(s => s.Id == id) is not { } squad)
            return;

        station.Comp.Squads.Remove(squad);
        station.Comp.Orders.RemoveAll(o => o.Squad == id);
        station.Comp.Tasks.RemoveAll(t => t.Squad == id);
        foreach (var member in squad.Members)
        {
            ClearSquad(member);
        }

        RefreshSummary((station, station.Comp));
        UpdateTablets(station);
    }

    /// <summary>
    /// Moves the member to the squad, out of their previous one. A null squad leaves them free.
    /// </summary>
    public bool TrySetSquad(StationRecordKey member, int? squadId)
    {
        if (!TryComp<StationSquadsComponent>(member.OriginStation, out var comp))
            return false;

        Entity<StationSquadsComponent> squads = (member.OriginStation, comp);
        var target = squadId == null ? null : comp.Squads.Find(s => s.Id == squadId);
        if (squadId != null && target == null)
            return false;

        HashSet<SecuritySquad> touched = [];
        MoveMember(squads, member, target, touched);
        RefreshTouched(squads, touched);
        return true;
    }

    public bool TryGetSquad(StationRecordKey member, [NotNullWhen(true)] out SecuritySquad? squad)
    {
        return TryGetSquad(member, out _, out squad);
    }

    /// <summary>
    /// Whether the job can be put in a squad: any security job except the head of security.
    /// </summary>
    public bool IsSquadCandidate(string job)
    {
        return job != HeadOfSecurity && IsSecurity(job);
    }

    #endregion

    #region Helpers

    private bool IsSecurity(string job)
    {
        return _proto.Index(SecurityDepartment).Roles.Contains(job);
    }

    private bool TryGetSquad(StationRecordKey member,
        out Entity<StationSquadsComponent> squads,
        [NotNullWhen(true)] out SecuritySquad? squad)
    {
        squads = default;
        squad = null;
        if (!TryComp<StationSquadsComponent>(member.OriginStation, out var comp))
            return false;

        squads = (member.OriginStation, comp);
        squad = comp.Squads.Find(s => s.Members.Contains(member));
        return squad != null;
    }

    /// <summary>
    /// Bodies of the members, by record. A member can have none.
    /// </summary>
    private Dictionary<StationRecordKey, Entity<SecurityNeuroComponent>> GetMobs(EntityUid station)
    {
        Dictionary<StationRecordKey, Entity<SecurityNeuroComponent>> mobs = [];
        var query = EntityQueryEnumerator<SecurityNeuroComponent>();
        while (query.MoveNext(out var uid, out var neuro))
        {
            if (neuro.Record is { } key && key.OriginStation == station)
                mobs[key] = (uid, neuro);
        }

        return mobs;
    }

    private MobState GetState(Dictionary<StationRecordKey, Entity<SecurityNeuroComponent>> mobs, StationRecordKey key)
    {
        return mobs.TryGetValue(key, out var mob) && _mobStateQuery.TryComp(mob, out var mobState)
            ? mobState.CurrentState
            : MobState.Dead;
    }

    /// <summary>
    /// Sends every member with a body the names and states of the others, the squad's orders and tasks.
    /// A member without a body counts as dead.
    /// </summary>
    private void RefreshSquad(Entity<StationSquadsComponent> squads, SecuritySquad squad)
    {
        var mobs = GetMobs(squads);

        List<(StationRecordKey Key, SecurityNeuroSquadMember Member)> members = [];
        foreach (var key in squad.Members)
        {
            if (!_records.TryGetRecord<GeneralStationRecord>(key, out var record))
                continue;

            members.Add((key, new SecurityNeuroSquadMember { Name = record.Name, State = GetState(mobs, key) }));
        }

        List<SecurityNeuroMark> marks = [];
        foreach (var order in squads.Comp.Orders)
        {
            if (order.Squad != squad.Id)
                continue;

            marks.Add(new SecurityNeuroMark
            {
                Id = order.Id,
                Order = order.Order,
                Location = order.Location,
                Color = squad.Color,
                Coordinates = GetNetCoordinates(order.Coordinates),
            });
        }

        var wanted = GetWanted(squads);
        foreach (var key in squad.Members)
        {
            if (!mobs.TryGetValue(key, out var mob))
                continue;

            List<SecurityNeuroSquadMember> others = [];
            foreach (var other in members)
            {
                if (!other.Key.Equals(key))
                    others.Add(other.Member);
            }

            // Without a link the squad and its orders stay as they were last seen.
            if (HasComp<NeuroSignalLostComponent>(mob))
            {
                RefreshTasks(mob, wanted);
                continue;
            }

            if (!others.SequenceEqual(mob.Comp.Squad))
            {
                mob.Comp.Squad = others;
                DirtyField(mob, mob.Comp, nameof(SecurityNeuroComponent.Squad));
            }

            // The commander outlines every squad, RefreshSummary sets that list.
            if (!mob.Comp.Commander)
            {
                List<NetEntity> allies = [];
                foreach (var other in squad.Members)
                {
                    if (!other.Equals(key) && mobs.TryGetValue(other, out var body))
                        allies.Add(GetNetEntity(body));
                }

                SetAllies(mob, allies);
            }

            if (!marks.SequenceEqual(mob.Comp.Marks))
            {
                mob.Comp.Marks = [.. marks];
                DirtyField(mob, mob.Comp, nameof(SecurityNeuroComponent.Marks));
            }

            RefreshTasks(mob, wanted);
        }

        RefreshSummary(squads, mobs);
    }

    /// <summary>
    /// Moves the member without refreshing anyone, the squads that changed are added to <paramref name="touched"/>.
    /// </summary>
    private void MoveMember(Entity<StationSquadsComponent> squads,
        StationRecordKey member,
        SecuritySquad? target,
        HashSet<SecuritySquad> touched)
    {
        var current = squads.Comp.Squads.Find(s => s.Members.Contains(member));
        if (current == target)
            return;

        if (current != null)
        {
            current.Members.Remove(member);
            touched.Add(current);
        }

        if (target == null)
            ClearSquad(member);
        else
        {
            target.Members.Add(member);
            touched.Add(target);
        }
    }

    private void RefreshTouched(Entity<StationSquadsComponent> squads, HashSet<SecuritySquad> touched)
    {
        if (touched.Count == 0)
            return;

        foreach (var squad in touched)
        {
            RefreshSquad(squads, squad);
        }

        UpdateTablets(squads);
    }

    private void ClearSquad(StationRecordKey member)
    {
        var query = EntityQueryEnumerator<SecurityNeuroComponent>();
        while (query.MoveNext(out var uid, out var neuro))
        {
            if (neuro.Record is not { } key
                || !key.Equals(member))
                continue;

            if (neuro.Squad.Count > 0)
            {
                neuro.Squad.Clear();
                DirtyField(uid, neuro, nameof(SecurityNeuroComponent.Squad));
            }

            if (neuro.Marks.Count > 0)
            {
                neuro.Marks.Clear();
                DirtyField(uid, neuro, nameof(SecurityNeuroComponent.Marks));
            }

            if (!neuro.Commander)
                SetAllies((uid, neuro), []);

            RefreshTasks((uid, neuro));
        }
    }

    #endregion
}
