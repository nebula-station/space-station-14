// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Logs;
using Content.Shared._Nebula.CCVar;
using Content.Shared._Nebula.InfinityDorms;
using Content.Shared._Nebula.InfinityDorms.Components;
using Content.Shared._Nebula.Teleportation.Events;
using Content.Shared.Access.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.AlertLevel;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Station.Systems;
using Content.Shared.Tag;
using Content.Trauma.Common.Effects;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Nebula.InfinityDorm;

public sealed partial class InfinityDormSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private MapLoaderSystem _loader = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private AlertLevelSystem _alertLevel = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private CommonSparksSystem _sparks = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private IConfigurationManager _cfgManager = default!;

    private static readonly TimeSpan WorldDeletingCheckDelay = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Key = griduid | Value = current world visitors data
    /// </summary>
    private Dictionary<EntityUid, List<InfinityDormServerVisitorData>> _mapVisitors = [];

    /// <summary>
    /// Key = userUid, value = [ GridUid ]
    /// </summary>
    private Dictionary<EntityUid, List<EntityUid>> _currentUserWorlds = [];

    private Dictionary<EntityUid, TimeSpan> _nextTeleporterAction = [];
    private Dictionary<EntityUid, TimeSpan> _nextExitAction = [];
    private TimeSpan _nextWorldDeleteCheck = TimeSpan.Zero;

    private LinkedList<(EntityUid Uid, TimeSpan Deadline)> _worldDeleteQueue = [];
    private Dictionary<EntityUid, LinkedListNode<(EntityUid Uid, TimeSpan Deadline)>> _worldDeleteNodes = [];

    private TimeSpan _roomDeleteTimer = TimeSpan.Zero;
    private int _roomsLimit = 1;
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _cfgManager.OnValueChanged(NCCVars.RoomDeleteTimer, value => _roomDeleteTimer = TimeSpan.FromMinutes(value), true);
        _cfgManager.OnValueChanged(NCCVars.RoomsLimit, value => _roomsLimit = value, true);

        _sawmill = _logManager.GetSawmill("infinity-dorm");
    }

    #region Component events

    [SubscribeLocalEvent]
    private void OnRoundCleanUp(RoundRestartCleanupEvent _)
    {
        _dormsMapId = MapId.Nullspace;
        _lastPosition = -1000f;
        _nextTeleporterAction.Clear();
        _nextExitAction.Clear();
        _mapVisitors.Clear();
    }

    [SubscribeLocalEvent]
    private void OnVisitorShutdown(Entity<InfinityDormVisitorComponent> ent, ref ComponentShutdown args)
    {
        foreach (var list in _mapVisitors.Values)
            list.RemoveAll(d => d.UserId == ent.Owner);
    }

    #endregion

    #region Validation

    private bool CheckRateLimit(EntityUid actor, float cooldown, Dictionary<EntityUid, TimeSpan> actionDict)
    {
        var now = _timing.CurTime;

        if (actionDict.TryGetValue(actor, out var next) && now < next)
        {
            _popup.PopupEntity(Loc.GetString("infinity-dorm-teleporter-popup-rate-limit"), actor,
                actor, PopupType.MediumCaution);

            return false;
        }

        actionDict[actor] = now + TimeSpan.FromSeconds(cooldown);
        return true;
    }

    private bool IsAlertCodeNotAllowed(Entity<InfinityDormTeleporterComponent> ent,
        [NotNullWhen(true)] out AlertLevelPrototype? levelPrototype)
    {
        levelPrototype = null;


        if (ent.Comp.WhitelistedAlertLevel.Count == 0)
            return false;

        if (_station.GetOwningStation(ent.Owner) is not { } station
            || !_alertLevel.TryGetLevel(station, out var level)
            || !_proto.TryIndex(level, out levelPrototype))
            return false;

        return ent.Comp.WhitelistedAlertLevel.Contains(level.Value);
    }

    #endregion
    #region Security

    [SubscribeLocalEvent]
    private void OnHandTeleporterPortalAttempt(Entity<InfinityDormVisitorComponent> ent, ref HandTeleporterPortalAttemptEvent args)
    {
        args.Cancel("infinity-dorm-system-hand-teleport-attempt");
    }

    #endregion
    #region AutoDelete

    private bool CheckIsGridEmpty(EntityUid grid)
    {
        if (_mapVisitors.TryGetValue(grid, out var visitors) && visitors.Count > 0)
            return false;

        var query = EntityQueryEnumerator<InfinityDormVisitorComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid == grid)
                return false;
        }

        return true;
    }

    private void AddWorldToDeleteTimer(EntityUid uid)
    {
        if (_worldDeleteNodes.ContainsKey(uid))
            return;

        if (!TryComp<InfinityDormComponent>(uid, out var comp))
            return;

        var deadline = _timing.CurTime + _roomDeleteTimer;

        var node = _worldDeleteQueue.AddLast((uid, deadline));
        _worldDeleteNodes[uid] = node;
    }

    private void RemoveWorldFromDeleteTimer(EntityUid uid)
    {
        if (!_worldDeleteNodes.Remove(uid, out var node))
            return;

        _worldDeleteQueue.Remove(node);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_nextWorldDeleteCheck > _timing.CurTime)
            return;

        _nextWorldDeleteCheck = _timing.CurTime + WorldDeletingCheckDelay;

        while (_worldDeleteQueue.First is { } first &&
            first.Value.Deadline <= _timing.CurTime)
        {
            var (uid, _) = first.Value;

            DeleteGridIfWithoutPlayers(uid);

            _worldDeleteQueue.RemoveFirst();
            _worldDeleteNodes.Remove(uid);
        }
    }

    #endregion

}
