// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Shared._Nebula.InfinityDorm.Messages;
using Content.Shared._Nebula.InfinityDorms;
using Content.Shared._Nebula.InfinityDorms.Components;
using Content.Shared._Nebula.InfinityDorms.Prototypes;
using Content.Shared._Nebula.InfinityDorms.States;
using Content.Shared.Access.Components;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Nebula.InfinityDorm;

public sealed partial class InfinityDormSystem : EntitySystem
{

    private Dictionary<EntityUid, TimeSpan> _nextTeleporterAction = [];

    private Dictionary<EntityUid, List<InfinityDormServerVisitorData>> _mapVisitors = [];

    private readonly int _step = 100;
    private float _lastPosition = -1000f;
    private MapId _dormsMapId = MapId.Nullspace;

    #region Component events

    [SubscribeLocalEvent]
    private void OnMapShutdown(Entity<InfinityDormComponent> ent, ref ComponentShutdown args)
    {
        if (_mapVisitors.ContainsKey(ent.Owner))
            _mapVisitors.Remove(ent.Owner);
    }

    #endregion

    #region BUI events

    [SubscribeLocalEvent]
    private void OnTeleporterUiOpened(Entity<InfinityDormTeleporterComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!args.UiKey.Equals(InfinityDormTeleporterUiKey.Key))
            return;

        UpdateTeleporterUi(ent);
    }

    private void UpdateTeleporterUi(Entity<InfinityDormTeleporterComponent> ent)
    {
        var players = new List<List<InfinityDormVisitorData>>();

        foreach (var (_, grid) in ent.Comp.ConnectedMaps)
        {
            if (!Exists(grid))
                continue;

            if (!_mapVisitors.ContainsKey(grid))
                continue;

            List<InfinityDormVisitorData> visitorsOnMap = [];
            foreach (var data in _mapVisitors[grid])
            {
                visitorsOnMap.Add(data.VisitorData);
            }

            players.Add(visitorsOnMap);
        }

        _ui.SetUiState(ent.Owner, InfinityDormTeleporterUiKey.Key, new InfinityDormTeleportBUIState(players));
    }

    #endregion

    #region Handle events

    [SubscribeLocalEvent]
    private void HandleTeleporterMessage(Entity<InfinityDormTeleporterComponent> ent, ref InfinityDormTeleportMessage args)
    {
        if (!_actionBlocker.CanInteract(args.Actor, ent.Owner))
            return;

        if (!_interaction.InRangeUnobstructed(args.Actor, ent.Owner, ent.Comp.MaxInteractionDistance))
        {
            _popup.PopupEntity(Loc.GetString("infinity-dorm-teleporter-popup-distance"),
                ent.Owner, args.Actor, PopupType.MediumCaution);

            return;
        }

        if (!CheckRateLimit(args.Actor, ent.Comp.UsingRateLimit, _nextTeleporterAction))
            return;

        if (args.Number < ent.Comp.MinValueForRoomId || args.Number >= ent.Comp.MaxValueForRoomId)
        {
            _popup.PopupEntity(Loc.GetString("infinity-dorm-teleporter-popup-client-pidor"), ent.Owner,
                args.Actor, PopupType.MediumCaution);


            _adminLog.Add(LogType.Action, LogImpact.High,
                $"{ToPrettyString(args.Actor):user} sent invalid dorm number {args.Number} to {ToPrettyString(ent):teleporter} (possible modified client)");

            return;
        }

        if (IsAlertCodeNotAllowed(ent, out var levelPrototype))
        {
            _popup.PopupEntity(Loc.GetString("infinity-dorm-teleporter-popup-bad-alert-level",
                    ("code", levelPrototype.LocalizedName)), ent.Owner, args.Actor, PopupType.MediumCaution);

            return;
        }

        EnsureDormsMap();

        if (!TryCreateDorm(ent, ref args))
            return;

        if (!ent.Comp.ConnectedMaps.TryGetValue(args.Number, out var grid) || !Exists(grid))
            return;

        if (!TryComp<InfinityDormComponent>(grid, out var infinityDormComponent))
            return;

        if (infinityDormComponent.IsClosed && infinityDormComponent.Creator != args.Actor)
        {
            _popup.PopupEntity(Loc.GetString(messageId: "infinity-dorm-teleporter-popup-closed"),
                ent.Owner, args.Actor, PopupType.MediumCaution);

            return;
        }

        if (!TeleportToDorm(args.Actor, grid))
            return;

        var accessReader = EnsureComp<AccessReaderComponent>(ent.Owner);
        _accessReader.LogAccess((ent.Owner, accessReader), args.Actor);

        _sparks.DoSparks(Transform(args.Actor).Coordinates, ent.Owner);

        _adminLog.Add(LogType.Teleport, LogImpact.Low,
            $"{ToPrettyString(args.Actor):user} entered dorm #{args.Number} via {ToPrettyString(ent):teleporter}");

        MarkAllUserItemsWithTag(args.Actor, Comp<InfinityDormVisitorComponent>(args.Actor).NotInfinityDormItemTag);
    }

    #endregion
    #region Dorm creation and teleport

    private bool TryCreateDorm(Entity<InfinityDormTeleporterComponent> ent, ref InfinityDormTeleportMessage args)
    {
        if (IsDormExists(ent.Comp, args.Number))
            return true;

        if (!_proto.TryIndex<InfinityDormRoomPrototype>(args.Room, out var dormProto))
            return false;

        var offset = new Vector2(_lastPosition + _step, 0);
        if (!_loader.TryLoadGrid(_dormsMapId, dormProto.GridPath, out var grid, offset: offset))
        {
            _sawmill.Error($"Failed to load dorm grid '{dormProto.GridPath}' for room '{args.Room}'");
            return false;
        }

        _lastPosition = offset.X;

        var dormComp = EnsureComp<InfinityDormComponent>(grid.Value);
        dormComp.Creator = args.Actor;
        dormComp.Number = args.Number;
        dormComp.Coordinates = new EntityCoordinates(grid.Value, Transform(args.Actor).LocalPosition);
        dormComp.MapCoordinates = _transform.GetMapCoordinates(Transform(args.Actor));

        ent.Comp.ConnectedMaps[args.Number] = grid.Value.Owner;

        _sawmill.Debug($"Spawned dorm #{args.Number} at offset {offset}");

        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.Actor):user} created dorm #{args.Number} ({args.Room}) using {ToPrettyString(ent):teleporter}");

        if (_mapVisitors.ContainsKey(grid.Value))
            _mapVisitors.Remove(grid.Value);

        _mapVisitors.Add(grid.Value, []);

        return true;
    }

    private bool TeleportToDorm(EntityUid user, EntityUid grid)
    {
        var query = EntityQueryEnumerator<InfinityDormSpawnMarkerComponent, TransformComponent>();
        while (query.MoveNext(out var markerUid, out _, out var xform))
        {
            if (xform.GridUid != grid)
                continue;

            var visitor = EnsureComp<InfinityDormVisitorComponent>(user);
            visitor.ReturnCoordinates = Transform(user).Coordinates;

            _transform.SetMapCoordinates(user, _transform.GetMapCoordinates(markerUid, xform));

            if (_mapVisitors.TryGetValue(grid, out var list) && list.All(d => d.UserId != user))
                list.Add(GetServerDormVisitorData(user));

            return true;
        }

        Log.Error($"Dorm grid {ToPrettyString(grid)} has no spawn marker");
        return false;
    }

    #endregion
    #region Helpers

    private InfinityDormServerVisitorData GetServerDormVisitorData(EntityUid user)
    {
        var serverData = new InfinityDormServerVisitorData
        {
            VisitorData = new InfinityDormVisitorData
            {
                JobIconProtoId = "JobIconNoId",
                UserName = "Unknown"
            },
            UserId = user,
        };

        if (!_idCard.TryFindIdCard(user, out var card))
            return serverData;

        serverData.VisitorData.JobIconProtoId = card.Comp.JobIcon;
        serverData.VisitorData.UserName = card.Comp.FullName ?? "Unknown";

        return serverData;
    }

    private void EnsureDormsMap()
    {
        if (_map.TryGetMap(_dormsMapId, out _) || _dormsMapId != MapId.Nullspace)
            return;

        _dormsMapId = Transform(_map.CreateMap()).MapID;
    }

    private bool IsDormExists(InfinityDormTeleporterComponent comp, int number)
    {
        return comp.ConnectedMaps.TryGetValue(number, out var grid) && Exists(grid) && !TerminatingOrDeleted(grid);
    }

    private void MarkAllUserItemsWithTag(EntityUid user, ProtoId<TagPrototype> tagProto)
    {
        foreach (var item in _inventory.GetHandOrInventoryEntities(user).ToList())
        {
            MarkRecursive(item, tagProto);
        }
    }

    private void MarkRecursive(EntityUid item, ProtoId<TagPrototype> tagProto)
    {
        _tag.AddTag(item, tagProto);

        if (!TryComp<ContainerManagerComponent>(item, out var manager))
            return;

        foreach (var container in _container.GetAllContainers(item, manager))
        {
            foreach (var contained in container.ContainedEntities)
            {
                MarkRecursive(contained, tagProto);
            }
        }
    }

    #endregion
}
