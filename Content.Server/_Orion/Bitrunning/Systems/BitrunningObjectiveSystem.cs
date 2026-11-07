// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Server/_Orion/Bitrunning/Systems/BitrunningObjectiveSystem.cs

using System.Linq;
using Content.Trauma.Common.Effects;
using Content.Goobstation.Shared.Fishing.Events;
using Content.Shared._Orion.Bitrunning;
using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Robust.Server.Containers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Orion.Bitrunning.Systems;

public sealed partial class BitrunningObjectiveSystem : EntitySystem
{
    private static readonly TimeSpan SatiationUpdateInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextSatiationUpdate;

    [Dependency] private QuantumServerSystem _server = default!;
    [Dependency] private ByteforgeSystem _byteforge = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SatiationSystem _satiation = default!;

    private static readonly SatiationValue Overfed = "Overfed";
    private static readonly SatiationValue Overhydrated = "Overhydrated";
    [Dependency] private CommonSparksSystem _sparks = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ContainerSystem _container = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextSatiationUpdate)
            return;

        _nextSatiationUpdate = _timing.CurTime + SatiationUpdateInterval;

        var servers = EntityQueryEnumerator<QuantumServerComponent>();
        while (servers.MoveNext(out var serverUid, out var server))
        {
            if (server.State != BitrunningServerState.Running
                || server.ObjectiveCompleted
                || server.ObjectivePoints > 0
                || (server.ObjectiveType != BitrunningObjectiveType.FillStomach
                    && server.ObjectiveType != BitrunningObjectiveType.OverhydrateStomach))
                continue;

            if (_timing.CurTime < server.NextSatiationProgressTime)
                continue;

            foreach (var avatar in server.ActiveConnections)
            {
                if (!IsAvatarMeetingSatiationObjective(avatar, server.ObjectiveType))
                    continue;

                _server.AddObjectiveProgress(serverUid, 1);
                server.NextSatiationProgressTime = _timing.CurTime + TimeSpan.FromSeconds(1);
                break;
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnExitCollide(Entity<BitrunningExitMarkerComponent> ent, ref StartCollideEvent args)
    {
        if (!HasComp<AvatarConnectionComponent>(args.OtherEntity))
            return;

        if (!TryResolveDomainMapUid(ent.Owner, args.OtherEntity, out var mapUid))
            return;

        if (!_server.TryGetServerByDomainMap(mapUid, out _, out _))
            return;

        _server.TryRequestDisconnectAvatar(args.OtherEntity, args.OtherEntity, true);
    }

    [SubscribeLocalEvent]
    private void OnInteract(Entity<BitrunningObjectivePointComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!TryResolveDomainMapUid(ent.Owner, args.User, out var mapUid, out var coordinates))
            return;

        if (!_server.TryGetServerByDomainMap(mapUid, out var serverUid, out var server))
            return;

        if (server.ObjectiveType != BitrunningObjectiveType.CollectEncryptedCaches)
            return;

        _server.AddObjectiveProgress(serverUid, ent.Comp.Points);
        _audio.PlayPvs(ent.Comp.PickupSound, coordinates);
        if (ent.Comp.ConsumeOnUse)
            QueueDel(ent.Owner);

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnDeliveryCollide(Entity<BitrunningObjectiveDeliveryPointComponent> ent, ref StartCollideEvent args)
    {
        if (!TryResolveDomainMapUid(ent.Owner, args.OtherEntity, out var mapUid))
            return;

        if (!_server.TryGetServerByDomainMap(mapUid, out var serverUid, out var server))
            return;

        if (!HasComp<BitrunningObjectiveCargoComponent>(args.OtherEntity))
            return;

        if (HasComp<BitrunningDeliveredObjectiveCargoComponent>(args.OtherEntity))
            return;

        if (!_byteforge.HasLinkedByteforge(serverUid, server))
        {
            if (TryComp<MapComponent>(mapUid, out var mapComp))
                _popup.PopupEntity(Loc.GetString("bitrunning-delivery-byteforge-required"), ent, Filter.BroadcastMap(mapComp.MapId), true, PopupType.LargeCaution);

            return;
        }

        if (!_byteforge.TryDeliverObjectiveCargoToByteforge(serverUid, args.OtherEntity))
            return;

        _sparks.DoSparks(Transform(ent).Coordinates);

        if (server.ObjectiveType == BitrunningObjectiveType.DeliveryCacheCrate)
            _server.AddObjectiveProgress(serverUid, ent.Comp.Points);
    }

    [SubscribeLocalEvent]
    private void OnEnemyObjectiveStartup(Entity<BitrunningDomainEnemyObjectiveComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.DomainMapUid != null)
            return;

        if (!TryComp(ent, out TransformComponent? xform) || xform.MapUid is not { } mapUid)
            return;

        ent.Comp.DomainMapUid = mapUid;
    }

    [SubscribeLocalEvent]
    private void OnEnemyStateChanged(Entity<BitrunningDomainEnemyObjectiveComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        TryAwardEnemyEliminationProgress(ent);
    }

    [SubscribeLocalEvent]
    private void OnEnemyTerminating(Entity<BitrunningDomainEnemyObjectiveComponent> ent, ref EntityTerminatingEvent args)
    {
        TryAwardEnemyEliminationProgress(ent);
    }

    private void TryAwardEnemyEliminationProgress(Entity<BitrunningDomainEnemyObjectiveComponent> ent)
    {
        if (HasComp<BitrunningEnemyObjectiveCountedComponent>(ent))
            return;

        var resolvedMapUid = ent.Comp.DomainMapUid;
        EntityUid mapUid;

        if (resolvedMapUid is { } storedMapUid)
            mapUid = storedMapUid;
        else if (!TryResolveDomainMapUid(ent.Owner, null, out mapUid))
            return;

        if (!_server.TryGetServerByDomainMap(mapUid, out var serverUid, out var server))
            return;

        if (server.State != BitrunningServerState.Running)
            return;

        if (resolvedMapUid == null)
            ent.Comp.DomainMapUid = mapUid;

        if (server.ObjectiveType != BitrunningObjectiveType.EliminateEnemies)
            return;

        EnsureComp<BitrunningEnemyObjectiveCountedComponent>(ent);
        _server.AddObjectiveProgress(serverUid, ent.Comp.Points);
    }

    [SubscribeLocalEvent]
    private void OnFishCaught(Entity<AvatarConnectionComponent> ent, ref FishCaughtEvent args)
    {
        if (!TryResolveDomainMapUid(ent.Owner, null, out var mapUid))
            return;

        if (!_server.TryGetServerByDomainMap(mapUid, out var serverUid, out var server))
            return;

        if (server.ObjectiveType != BitrunningObjectiveType.CatchFish)
            return;

        _server.AddObjectiveProgress(serverUid, 1);
    }

    [SubscribeLocalEvent]
    private void OnRewardCacheOpened(Entity<BitrunningDespawnOnOpenComponent> ent, ref StorageAfterOpenEvent args)
    {
        var dropCoordinates = Transform(ent).Coordinates;
        EjectEntitiesFromStorage(ent, dropCoordinates);
        _sparks.DoSparks(dropCoordinates);
        QueueDel(ent);
    }

    private void EjectEntitiesFromStorage(EntityUid cargoUid, EntityCoordinates dropCoordinates)
    {
        if (TryComp<StorageComponent>(cargoUid, out var storage))
            _container.EmptyContainer(storage.Container, destination: dropCoordinates);

        if (!TryComp<EntityStorageComponent>(cargoUid, out var entityStorage))
            return;

        foreach (var contained in entityStorage.Contents.ContainedEntities.ToList())
        {
            _container.Remove(contained, entityStorage.Contents, destination: dropCoordinates, reparent: true);
        }
    }

    private bool TryResolveDomainMapUid(EntityUid primaryUid, EntityUid? fallbackUid, out EntityUid mapUid, out EntityCoordinates coordinates)
    {
        coordinates = default;
        if (TryComp(primaryUid, out TransformComponent? primaryXform) && primaryXform.MapUid is { } primaryMapUid)
        {
            mapUid = primaryMapUid;
            coordinates = primaryXform.Coordinates;
            return true;
        }

        if (fallbackUid != null && TryComp(fallbackUid.Value, out TransformComponent? fallbackXform) && fallbackXform.MapUid is { } fallbackMapUid)
        {
            mapUid = fallbackMapUid;
            coordinates = fallbackXform.Coordinates;
            return true;
        }

        mapUid = default;
        return false;
    }

    private bool TryResolveDomainMapUid(EntityUid primaryUid, EntityUid? fallbackUid, out EntityUid mapUid)
    {
        return TryResolveDomainMapUid(primaryUid, fallbackUid, out mapUid, out _);
    }

    private bool IsAvatarMeetingSatiationObjective(EntityUid avatarUid, BitrunningObjectiveType objectiveType)
    {
        return objectiveType switch
        {
            BitrunningObjectiveType.FillStomach => TryComp<SatiationComponent>(avatarUid, out var hunger) &&
                                                   _satiation.IsValueInRange((avatarUid, hunger), SatiationSystem.Hunger, above: Overfed),
            BitrunningObjectiveType.OverhydrateStomach => TryComp<SatiationComponent>(avatarUid, out var thirst) &&
                                                          _satiation.IsValueInRange((avatarUid, thirst), SatiationSystem.Thirst, above: Overhydrated),
            _ => false,
        };
    }
}
