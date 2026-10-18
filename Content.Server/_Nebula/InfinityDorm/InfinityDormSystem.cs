// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Logs;
using Content.Shared._Nebula.InfinityDorms.Components;
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

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

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
}
