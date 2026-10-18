// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Nebula.InfinityDorm.Messages;
using Content.Shared._Nebula.InfinityDorms;
using Content.Shared._Nebula.InfinityDorms.Components;
using Content.Shared._Nebula.InfinityDorms.States;
using Content.Shared.Database;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;


namespace Content.Server._Nebula.InfinityDorm;

public sealed partial class InfinityDormSystem
{
    private Dictionary<EntityUid, TimeSpan> _nextExitAction = [];

    #region BUI events

    [SubscribeLocalEvent]
    private void OnExitUiOpened(Entity<InfinityDormExitComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!args.UiKey.Equals(InfinityDormExitUiKey.Key))
            return;

        var gridUid = Transform(ent.Owner).GridUid;
        if (!TryComp<InfinityDormComponent>(gridUid, out var dormComponent))
            return;

        if (!_mapVisitors.ContainsKey(gridUid.Value))
            return;

        var state = new InfinityDormExitBUIState(
            dormComponent.IsClosed, dormComponent.Creator == args.Actor, _mapVisitors[gridUid.Value].Count);
        _ui.SetUiState(ent.Owner, InfinityDormExitUiKey.Key, state);
    }

    [SubscribeLocalEvent]
    private void OnToggleClosedMessage(Entity<InfinityDormExitComponent> ent, ref InfinityDormToggleClosedMessage args)
    {
        if (!CheckRateLimit(args.Actor, ent.Comp.UsingRateLimit, _nextExitAction))
            return;

        var transform = Transform(ent.Owner);
        if (!TryComp<InfinityDormComponent>(transform.GridUid, out var infinityDorm))
            return;

        infinityDorm.IsClosed = args.IsClosed;
    }


    [SubscribeLocalEvent]
    private void OnExitMessage(Entity<InfinityDormExitComponent> ent, ref InfinityDormExitMessage args)
    {
        var actor = args.Actor;

        if (Transform(ent.Owner).GridUid is not { } grid
            || !TryComp<InfinityDormComponent>(grid, out var dorm)
            || !TryComp<InfinityDormVisitorComponent>(actor, out var visitor))
            return;

        var target = visitor.ReturnCoordinates;
        if (!Exists(target.EntityId))
        {
            if (!Exists(dorm.TeleporterUid))
                return;

            target = Transform(dorm.TeleporterUid).Coordinates;
        }
        _transform.SetCoordinates(actor, target);

        DeleteAllUserItemsWithTag(actor, visitor.NotInfinityDormItemTag);

        if (_mapVisitors.TryGetValue(grid, out var list))
            list.RemoveAll(d => d.UserId == actor);

        RemComp<InfinityDormVisitorComponent>(actor);

        DeleteGridIfWithoutPlayers(grid);

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"User ({ToPrettyString(ent.Owner)}) exited from infinity dorm");
    }

    #endregion

    #region Helpers

    private void DeleteAllUserItemsWithTag(EntityUid user, ProtoId<TagPrototype> tagProto)
    {
        foreach (var item in _inventory.GetHandOrInventoryEntities(user).ToList())
        {
            DeleteRecursive(item, tagProto);
        }
    }

    private void DeleteRecursive(EntityUid item, ProtoId<TagPrototype> tag)
    {
        if (!_tag.HasTag(item, tag))
        {
            QueueDel(item);
            return;
        }

        _tag.RemoveTag(item, tag);

        if (!TryComp<ContainerManagerComponent>(item, out var manager))
            return;

        foreach (var container in _container.GetAllContainers(item, manager))
            foreach (var contained in container.ContainedEntities.ToList())
                DeleteRecursive(contained, tag);
    }

    private void DeleteGridIfWithoutPlayers(EntityUid grid)
    {
        if (!TryComp<InfinityDormComponent>(grid, out var dorm))
            return;

        if (_mapVisitors.TryGetValue(grid, out var visitors) && visitors.Count > 0)
            return;

        var query = EntityQueryEnumerator<InfinityDormVisitorComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid == grid)
                return;
        }

        if (TryComp<InfinityDormTeleporterComponent>(dorm.TeleporterUid, out var teleporter))
            teleporter.ConnectedMaps.Remove(dorm.Number);

        _mapVisitors.Remove(grid);

        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"Dorm #{dorm.Number} ({ToPrettyString(grid)}) deleted: no players left");

        QueueDel(grid);
    }
    #endregion

}
