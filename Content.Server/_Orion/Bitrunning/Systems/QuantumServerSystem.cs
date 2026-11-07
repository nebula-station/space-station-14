// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Server/_Orion/Bitrunning/Systems/QuantumServerSystem.cs

using System.Linq;
using System.Numerics;
using Content.Trauma.Common.Effects;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server._Orion.Bitrunning.Components;
using Content.Server.Actions;
using Content.Server.Antag.Components;
using Content.Server.Chat.Systems;
using Content.Server.Clothing.Systems;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DoAfter;
using Content.Server.Ghost;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mobs;
using Content.Server.NPC.HTN;
using Content.Server.Polymorph.Components;
using Content.Server.Preferences.Managers;
using Content.Server.Stunnable;
using Content.Server.SurveillanceCamera;
using Content.Lavaland.Common.Mobs;
using Content.Shared._Orion.Bitrunning;
using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared._Orion.Bitrunning.Prototypes;
using Content.Shared._Orion.Bitrunning.Systems;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Parallax;
using Content.Shared.Polymorph;
using Content.Shared.SurveillanceCamera.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Preferences;
using Content.Shared.Station.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Orion.Bitrunning.Systems;

public sealed partial class QuantumServerSystem : EntitySystem
{
    [Dependency] private BitrunningDomainSystem _domains = default!;
    [Dependency] private MapSystem _map = default!;
    [Dependency] private MapLoaderSystem _mapLoader = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private NetpodSystem _netpod = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private OutfitSystem _outfit = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private StunSystem _stun = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private BitrunningPointsSystem _bitrunningPoints = default!;
    [Dependency] private ByteforgeSystem _byteforge = default!;
    [Dependency] private BitrunningDiskSystem _bitrunningDisk = default!;
    [Dependency] private SurveillanceCameraSystem _surveillanceCamera = default!;
    [Dependency] private IServerPreferencesManager _preferences = default!;
    [Dependency] private StationSpawningSystem _spawning = default!; // Nebula edit
    [Dependency] private CommonSparksSystem _sparks = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private DeathgaspSystem _deathgasp = default!;
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private QuantumConsoleSystem _console = default!;

    private static readonly EntProtoId ExitBlindnessStatusEffect = "StatusEffectBitrunningExitBlindness";
    private const string ServerSourcePort = "BitrunningServerSource";

    private static readonly TimeSpan DisconnectActionBlockDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DisconnectActionDoAfterDuration = TimeSpan.FromSeconds(5);

    // Nebula start
    private static readonly TimeSpan CooldownCheckInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextCooldownCheck;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextCooldownCheck)
            return;

        _nextCooldownCheck = _timing.CurTime + CooldownCheckInterval;

        var query = EntityQueryEnumerator<QuantumServerComponent>();
        while (query.MoveNext(out var uid, out var server))
        {
            if (server.State != BitrunningServerState.CoolingDown || _timing.CurTime < server.CooldownEndTime)
                continue;

            server.State = BitrunningServerState.Ready;
            server.CooldownEndTime = TimeSpan.Zero;
            UpdateServerVisuals((uid, server));
            Dirty(uid, server);
        }
    }
    // Nebula end

    [SubscribeLocalEvent]
    private void OnServerInit(Entity<QuantumServerComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(ent.Owner, ServerSourcePort);
        UpdateServerVisuals(ent);
    }

    [SubscribeLocalEvent]
    private void OnServerMapInit(Entity<QuantumServerComponent> ent, ref MapInitEvent args)
    {
        _byteforge.RefreshLinkedByteforge(ent);
        UpdateServerVisuals(ent);
    }

    [SubscribeLocalEvent]
    private void OnServerTerminating(Entity<QuantumServerComponent> ent, ref EntityTerminatingEvent args)
    {
        StopDomain(ent, true);
    }

    [SubscribeLocalEvent]
    private void OnServerInteractUsing(Entity<QuantumServerComponent> ent, ref InteractUsingEvent args)
    {
        if (ent.Comp.State != BitrunningServerState.Running)
            return;

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("bitrunning-server-active"), ent, args.User);
    }

    [SubscribeLocalEvent]
    private void OnServerPowerChanged(Entity<QuantumServerComponent> ent, ref PowerChangedEvent args)
    {
        UpdateServerVisuals(ent);

        if (args.Powered || ent.Comp.State != BitrunningServerState.Running)
            return;

        foreach (var connection in ent.Comp.ActiveConnections.ToArray())
        {
            DisconnectAvatar(connection, true);
        }
    }



    public bool TryColdBoot(EntityUid serverUid, string domainId, bool randomized = false)
    {
        if (!TryComp<QuantumServerComponent>(serverUid, out var server))
            return false;

        if (!_power.IsPowered(serverUid))
            return false;

        if (server.State != BitrunningServerState.Ready)
            return false;

        if (server.CurrentDomain != null)
            return false;

        if (!_domains.TryGetDomain(domainId, out var domain))
            return false;

        if (!CanAccessDomain(server, domain))
            return false;

        if (domain.Difficulty == BitrunningDifficulty.Extreme && !HasComp<EmaggedComponent>(serverUid))
            return false;

        if (server.Points < domain.Cost)
            return false;

        var mapEntity = _map.CreateMap(out var mapId, runMapInit: true);
        EnsureComp<BitrunningDomainRuntimeComponent>(mapEntity);
        _metaData.SetEntityName(mapEntity, Loc.GetString(domain.Name));

        var parallax = EnsureComp<ParallaxComponent>(mapEntity);
        parallax.Parallax = HasComp<EmaggedComponent>(serverUid)
            ? "CyberRed"
            : "Cyber";
        Dirty(mapEntity, parallax);

        if (!_mapLoader.TryLoadGrid(mapId, domain.MapPath, out var grid, offset: Vector2.Zero))
        {
            _map.DeleteMap(mapId);
            return false;
        }

        var objectiveType = PickObjectiveType(domain);

        server.DomainMapUid = mapEntity;
        server.DomainGridUid = grid.Value;
        server.CurrentDomain = domainId;
        server.State = BitrunningServerState.Running;
        server.DomainStartTime = _timing.CurTime;
        server.ObjectivePoints = 0;
        server.NextSatiationProgressTime = _timing.CurTime;
        server.ObjectiveType = objectiveType;
        server.ObjectiveGoal = ResolveObjectiveGoal((serverUid, server), domain, objectiveType);
        server.ObjectiveCompleted = false;
        server.Points -= domain.Cost;
        server.ThreatsSpawned = 0;
        server.CooldownEndTime = TimeSpan.Zero;
        server.AllowDiskModifications = domain.AllowDiskModifications;
        server.AllowProfileLoad = domain.AllowProfileLoad;
        server.WasRandomizedRun = randomized;
        server.GrantedItemDisks.Clear();

        ResolveDomainMarkers((serverUid, server));
        CleanupObjectiveArtifacts((serverUid, server));
        _audio.PlayPvs(server.DomainStartSound, serverUid);
        _audio.PlayPvs(server.DomainLoadedSound, serverUid);
        UpdateServerVisuals((serverUid, server));
        Dirty(serverUid, server);
        return true;
    }

    private void UpdateServerVisuals(Entity<QuantumServerComponent> serverEnt)
    {
        var visualState = !_power.IsPowered(serverEnt.Owner)
            ? QuantumServerVisualState.Unpowered
            : serverEnt.Comp.State switch
            {
                BitrunningServerState.Running => QuantumServerVisualState.Running,
                BitrunningServerState.CoolingDown => QuantumServerVisualState.Cooling,
                _ => QuantumServerVisualState.Ready,
            };

        _appearance.SetData(serverEnt, QuantumServerVisuals.QuantumServerState, visualState);
    }

    private void ResolveDomainMarkers(Entity<QuantumServerComponent> serverEnt)
    {
        serverEnt.Comp.ExitCoordinates = null;
        serverEnt.Comp.GoalCoordinates = null;
        serverEnt.Comp.CacheCoordinates = null;
        serverEnt.Comp.HasExplicitCacheMarker = false;
        serverEnt.Comp.SpawnCoordinates = null;

        if (serverEnt.Comp.DomainMapUid is not { } mapUid)
            return;

        var exits = EntityQueryEnumerator<BitrunningExitMarkerComponent, TransformComponent>();
        while (exits.MoveNext(out _, out _, out var xform))
        {
            if (xform.MapUid != mapUid)
                continue;

            serverEnt.Comp.ExitCoordinates ??= xform.Coordinates;
            break;
        }

        var goals = EntityQueryEnumerator<BitrunningGoalMarkerComponent, TransformComponent>();
        while (goals.MoveNext(out _, out _, out var xform))
        {
            if (xform.MapUid != mapUid)
                continue;

            serverEnt.Comp.GoalCoordinates ??= xform.Coordinates;
            break;
        }

        List<EntityCoordinates> rewardCacheCoordinates = [];
        var rewardCacheMarkers = EntityQueryEnumerator<BitrunningRewardCacheSpawnMarkerComponent, TransformComponent>();
        while (rewardCacheMarkers.MoveNext(out _, out _, out var xform))
        {
            if (xform.MapUid != mapUid)
                continue;

            rewardCacheCoordinates.Add(xform.Coordinates);
        }

        if (rewardCacheCoordinates.Count > 0)
        {
            serverEnt.Comp.CacheCoordinates = rewardCacheCoordinates[0];
            serverEnt.Comp.HasExplicitCacheMarker = true;
        }

        var spawnMarkers = EntityQueryEnumerator<BitrunningAvatarSpawnMarkerComponent, TransformComponent>();
        while (spawnMarkers.MoveNext(out _, out _, out var xform))
        {
            if (xform.MapUid != mapUid)
                continue;

            serverEnt.Comp.SpawnCoordinates ??= xform.Coordinates;
            break;
        }

        if (serverEnt.Comp.ExitCoordinates == null && serverEnt.Comp.DomainGridUid is { } gridUid)
        {
            serverEnt.Comp.ExitCoordinates = TryComp<MapGridComponent>(gridUid, out var gridComp)
                ? new EntityCoordinates(gridUid, gridComp.LocalAABB.Center)
                : new EntityCoordinates(gridUid, Vector2.Zero);
        }

        serverEnt.Comp.GoalCoordinates ??= serverEnt.Comp.ExitCoordinates;
        serverEnt.Comp.CacheCoordinates ??= serverEnt.Comp.GoalCoordinates;
        serverEnt.Comp.SpawnCoordinates ??= serverEnt.Comp.ExitCoordinates;

        if (serverEnt.Comp.GoalCoordinates == null)
            return;

        var hasObjective = HasActiveObjective(serverEnt.Comp);
        if (!hasObjective)
            return;

        var isModular = TryGetCurrentDomain(serverEnt.Comp, out var domain) && domain?.IsModular == true;
        var objectiveTarget = Math.Max(1, serverEnt.Comp.ObjectiveGoal);

        switch (serverEnt.Comp.ObjectiveType)
        {
            case BitrunningObjectiveType.DeliveryCacheCrate:
                SpawnDeliveryObjectiveCaches(serverEnt, isModular, objectiveTarget);
                break;
            case BitrunningObjectiveType.CollectEncryptedCaches:
                SpawnEncryptedObjectiveCaches(serverEnt, isModular, objectiveTarget);
                break;
        }
    }

    private void SpawnDeliveryObjectiveCaches(Entity<QuantumServerComponent> serverEnt, bool isModular, int objectiveTarget)
    {
        List<(EntProtoId Prototype, EntityCoordinates Coordinates)> markerCandidates = [];
        var markers = EntityQueryEnumerator<BitrunningObjectiveCacheCrateSpawnMarkerComponent, TransformComponent>();
        while (markers.MoveNext(out _, out var marker, out var xform))
        {
            if (xform.MapUid != serverEnt.Comp.DomainMapUid)
                continue;

            markerCandidates.Add((marker.CratePrototype, xform.Coordinates));
        }

        SpawnObjectiveMarkers(markerCandidates, isModular, objectiveTarget);
    }

    private void SpawnEncryptedObjectiveCaches(Entity<QuantumServerComponent> serverEnt, bool isModular, int objectiveTarget)
    {
        List<(EntProtoId Prototype, EntityCoordinates Coordinates)> markerCandidates = [];
        var markers = EntityQueryEnumerator<BitrunningObjectiveEncryptedCacheSpawnMarkerComponent, TransformComponent>();
        while (markers.MoveNext(out _, out var marker, out var xform))
        {
            if (xform.MapUid != serverEnt.Comp.DomainMapUid)
                continue;

            markerCandidates.Add((marker.CachePrototype, xform.Coordinates));
        }

        SpawnObjectiveMarkers(markerCandidates, isModular, objectiveTarget);
    }

    private void SpawnObjectiveMarkers(List<(EntProtoId Prototype, EntityCoordinates Coordinates)> markerCandidates, bool isModular, int objectiveTarget)
    {
        if (markerCandidates.Count == 0)
            return;

        _random.Shuffle(markerCandidates);

        if (!isModular)
        {
            foreach (var marker in markerCandidates)
            {
                Spawn(marker.Prototype, marker.Coordinates);
                _sparks.DoSparks(marker.Coordinates);
            }

            return;
        }

        for (var i = 0; i < objectiveTarget; i++)
        {
            var marker = markerCandidates[i % markerCandidates.Count];
            Spawn(marker.Prototype, marker.Coordinates);
            _sparks.DoSparks(marker.Coordinates);
        }
    }

    public bool StopDomain(Entity<QuantumServerComponent> serverEnt, bool immediate = false)
    {
        // Nebula start
        if (!immediate && serverEnt.Comp.State != BitrunningServerState.Running)
            return false;
        // Nebula end

        serverEnt.Comp.State = immediate
            ? BitrunningServerState.Ready
            : BitrunningServerState.CoolingDown;

        if (serverEnt.Comp.ActiveConnections.Count > 0)
            _audio.PlayPvs(serverEnt.Comp.DomainAlertSound, serverEnt.Owner);
        else
            _audio.PlayPvs(serverEnt.Comp.DomainStopSound, serverEnt.Owner);

        foreach (var connection in serverEnt.Comp.ActiveConnections.ToArray())
        {
            PlayAvatarLocalSound(connection, serverEnt.Comp.DomainAlertSound);
            DisconnectAvatar(connection, false);
        }

        if (serverEnt.Comp.DomainMapUid is { } mapUid && !TerminatingOrDeleted(mapUid)) // Nebula edit
            QueueDel(mapUid); // Nebula edit

        serverEnt.Comp.DomainMapUid = null;
        serverEnt.Comp.DomainGridUid = null;
        serverEnt.Comp.CurrentDomain = null;
        serverEnt.Comp.ActiveConnections.Clear();
        serverEnt.Comp.ExitCoordinates = null;
        serverEnt.Comp.GoalCoordinates = null;
        serverEnt.Comp.CacheCoordinates = null;
        serverEnt.Comp.HasExplicitCacheMarker = false;
        serverEnt.Comp.SpawnCoordinates = null;
        serverEnt.Comp.ObjectivePoints = 0;
        serverEnt.Comp.ObjectiveCompleted = false;
        serverEnt.Comp.GrantedItemDisks.Clear();
        serverEnt.Comp.AllowProfileLoad = true;
        serverEnt.Comp.WasRandomizedRun = false;

        if (immediate)
        {
            serverEnt.Comp.CooldownEndTime = TimeSpan.Zero;
        }
        else
        {
            var delay = TimeSpan.FromSeconds(serverEnt.Comp.Cooldown.TotalSeconds * serverEnt.Comp.CooldownMultiplier);
            serverEnt.Comp.CooldownEndTime = _timing.CurTime + delay; // Nebula edit
        }

        UpdateServerVisuals(serverEnt);
        Dirty(serverEnt);
        return true;
    }

    public bool TryConnectRunner(EntityUid serverUid, EntityUid podUid, EntityUid user)
    {
        if (!TryComp<QuantumServerComponent>(serverUid, out var server) || !TryComp<NetpodComponent>(podUid, out var pod))
            return false;

        if (!_power.IsPowered(serverUid))
            return false;

        if (server.State != BitrunningServerState.Running)
            return false;

        if (pod.Occupant != null && pod.Occupant != user)
            return false;

        if (server.ExitCoordinates == null)
            return false;

        if (!_mind.TryGetMind(user, out var mindId, out var mind))
            return false;

        if (pod.Avatar != null)
        {
            if (TryReconnectRunner((podUid, pod), user))
                return true;

            if (pod.Avatar is { } oldAvatar && Exists(oldAvatar))
                DisconnectAvatar(oldAvatar, true);

            pod.Avatar = null;
            Dirty(podUid, pod);
        }

        var avatar = SpawnAvatarForRunner(server, user, server.SpawnCoordinates ?? server.ExitCoordinates.Value);
        EnsureComp<BitrunningDomainRuntimeComponent>(avatar);
        _metaData.SetEntityName(avatar, Name(user));

        var connection = EnsureComp<AvatarConnectionComponent>(avatar);
        connection.OriginalBody = user;
        connection.Server = serverUid;
        connection.Netpod = podUid;
        connection.RunnerMind = mindId;
        connection.NoHit = true;
        connection.DeleteOnDisconnect = GetDeleteOnDisconnect(server);
        EnsureComp<AvatarNavRelayComponent>(avatar).RelayEntity = podUid;
        EnsureComp<AvatarNavRelayComponent>(podUid).RelayEntity = avatar;
        _surveillanceCamera.RefreshViewerTargets(podUid); // Nebula edit

        _mind.TransferTo(mindId, avatar, mind: mind);
        PlayLocalSound(user, pod.ConnectStasisSound);
        PlayLocalSound(avatar, pod.ConnectAvatarSound);
        TryApplyAvatarOutfit(avatar, server, pod);
        SetAvatarBroadcastEnabled((avatar, connection), server, server.BroadcastEnabled);
        _actions.AddAction(avatar, ref connection.DisconnectActionEntity, connection.DisconnectActionPrototype, avatar);
        var objectivePopupText = server.ObjectiveCompleted
            ? Loc.GetString("bitrunning-objective-completed")
            : GetObjectiveInstructions(server);
        _popup.PopupEntity(objectivePopupText, avatar, avatar, PopupType.Large);

        pod.Occupant = user;
        pod.Avatar = avatar;
        pod.LinkedServer = serverUid;

        server.ActiveConnections.Add(avatar);

        Dirty(podUid, pod);
        _netpod.UpdateVisuals((podUid, pod));
        Dirty(serverUid, server);
        _bitrunningDisk.RefreshAvatarEffects(avatar);
        return true;
    }

    private bool TryReconnectRunner(Entity<NetpodComponent> pod, EntityUid user)
    {
        if (pod.Comp.Avatar is not { } avatarUid || !TryComp<AvatarConnectionComponent>(avatarUid, out var connection))
            return false;

        if (HasComp<ActorComponent>(avatarUid))
            return false;

        if (TryComp<MobStateComponent>(avatarUid, out var state) && state.CurrentState == MobState.Dead)
            return false;

        if (!_mind.TryGetMind(user, out var mindId, out var mind))
            return false;

        var isOriginalBodyReconnect = connection.OriginalBody == user;
        var isMindOwnerReconnect = connection.RunnerMind == mindId;
        if (!isOriginalBodyReconnect && !isMindOwnerReconnect)
            return false;

        _mind.TransferTo(mindId, avatarUid, mind: mind);
        _actions.AddAction(avatarUid, ref connection.DisconnectActionEntity, connection.DisconnectActionPrototype, avatarUid);
        EnsureComp<AvatarNavRelayComponent>(avatarUid).RelayEntity = pod.Owner;
        EnsureComp<AvatarNavRelayComponent>(pod.Owner).RelayEntity = avatarUid;
        _surveillanceCamera.RefreshViewerTargets(pod.Owner); // Nebula edit

        connection.Netpod = pod.Owner;
        connection.OriginalBody = user;
        if (pod.Comp.LinkedServer != null)
            connection.Server = pod.Comp.LinkedServer;

        if (connection.Server != null && TryComp<QuantumServerComponent>(connection.Server.Value, out var server))
        {
            server.ActiveConnections.Add(avatarUid);
            var objectivePopupText = server.ObjectiveCompleted
                ? Loc.GetString("bitrunning-objective-completed")
                : GetObjectiveInstructions(server);
            _popup.PopupEntity(objectivePopupText, avatarUid, avatarUid, PopupType.Large);
            Dirty(connection.Server.Value, server);
        }

        _bitrunningDisk.RefreshAvatarEffects(avatarUid);
        return true;
    }

    private bool GetDeleteOnDisconnect(QuantumServerComponent server)
    {
        if (server.CurrentDomain == null || !_domains.TryGetDomain(server.CurrentDomain, out var domain))
            return false;

        return domain.DeleteAvatarOnDisconnect;
    }

    private void TryApplyAvatarOutfit(EntityUid avatar, QuantumServerComponent server, NetpodComponent pod)
    {
        if (!TryResolveLoadout(server, pod, out var loadoutId))
            return;

        _outfit.SetOutfit(avatar, loadoutId);
    }

    private bool TryResolveLoadout(QuantumServerComponent server, NetpodComponent pod, out string loadout)
    {
        loadout = string.Empty;

        if (server.CurrentDomain != null &&
            _domains.TryGetDomain(server.CurrentDomain, out var domain) &&
            domain is { ForcedLoadout: not null })
        {
            loadout = domain.ForcedLoadout.Value;
            return true;
        }

        if (pod.PreferredLoadout == null)
            return false;

        loadout = pod.PreferredLoadout.Value;
        return true;

    }

    private void SetAvatarBroadcastEnabled(Entity<AvatarConnectionComponent> avatar, QuantumServerComponent server, bool enabled)
    {
        var cameraEntity = avatar.Owner;

        if (!enabled)
        {
            RemCompDeferred<SurveillanceCameraComponent>(cameraEntity);
            RemCompDeferred<DeviceNetworkComponent>(cameraEntity);
            RemCompDeferred<WirelessNetworkComponent>(cameraEntity);
            return;
        }

        EnsureComp<WirelessNetworkComponent>(cameraEntity).Range = server.BroadcastWirelessRange;

        var device = EnsureComp<DeviceNetworkComponent>(cameraEntity);
        device.NetIdEnum = DeviceNetIdDefaults.Wireless;
        EnsureComp<SurveillanceCameraComponent>(cameraEntity);
        _surveillanceCamera.ConfigureCameraNetwork(cameraEntity, "SurveillanceCameraEntertainment", "SurveillanceCamera");
    }

    public void DisconnectAvatar(EntityUid avatarUid, bool harmful, bool bodyEjected = false) // Nebula edit
    {
        if (!TryComp<AvatarConnectionComponent>(avatarUid, out var connection))
            return;

        harmful |= IsAvatarInCriticalState(avatarUid);

        ReleaseAvatarHands(avatarUid);

        var originalBody = connection.OriginalBody;
        var serverUid = connection.Server;
        var podUid = connection.Netpod;
        var canRedirectToBitrunner = bodyEjected && originalBody != null || CanRedirectToBitrunnerBody(connection, originalBody); // Nebula edit

        if (originalBody is { } bodyUid && podUid is { } netpodUid && TryComp<NetpodComponent>(netpodUid, out var podComp))
            PlayLocalSound(bodyUid, podComp.DisconnectSound);

        if (originalBody is { } bodyToTransfer && TryResolveRunnerMind((avatarUid, connection), out var mindId))
            _mind.TransferTo(mindId, bodyToTransfer);

        connection.OriginalBody = null;

        if (harmful && canRedirectToBitrunner && originalBody is { } redirectedBody && !IsAvatarInCriticalState(redirectedBody))
            TransferAvatarDamageToBitrunner((avatarUid, connection), redirectedBody, true);

        if (harmful && IsAvatarInCriticalState(avatarUid))
            KillAvatar(avatarUid);

        if (podUid != null && TryComp<NetpodComponent>(podUid.Value, out var pod))
        {
            if (TryComp<SurveillanceCameraComponent>(podUid.Value, out var camera))
                _surveillanceCamera.ClearActiveViewers(podUid.Value, camera);

            EnsureComp<AvatarNavRelayComponent>(podUid.Value).RelayEntity = null;

            pod.Occupant = TryComp<NetpodContainerComponent>(podUid.Value, out var containerComp)
                ? containerComp.BodyContainer.ContainedEntity
                : null;

            if (connection.DeleteOnDisconnect)
                pod.Avatar = null;

            Dirty(podUid.Value, pod);
            _netpod.UpdateVisuals((podUid.Value, pod));

            _netpod.EjectOccupant(podUid.Value);
        }

        ApplyBitrunningExitEffects(originalBody, serverUid);

        if (serverUid != null && TryComp<QuantumServerComponent>(serverUid.Value, out var server))
        {
            server.ActiveConnections.Remove(avatarUid);
            Dirty(serverUid.Value, server);
        }

        connection.Netpod = null;

        if (connection.DeleteOnDisconnect)
            QueueDel(avatarUid);

        if (HasComp<ActorComponent>(avatarUid))
            return;

        _actions.RemoveAction(connection.DisconnectActionEntity);
        connection.DisconnectActionEntity = null;
    }

    public void AddObjectiveProgress(EntityUid serverUid, int points)
    {
        if (!TryComp<QuantumServerComponent>(serverUid, out var server))
            return;

        if (server.State != BitrunningServerState.Running)
            return;

        if (server.ObjectiveCompleted)
            return;

        if (!HasActiveObjective(server))
            return;

        server.ObjectivePoints += points;
        if (server.ObjectivePoints >= server.ObjectiveGoal)
            CompleteObjective((serverUid, server));

        Dirty(serverUid, server);
    }

    public bool TryGetServerByDomainMap(EntityUid mapUid, out EntityUid serverUid, out QuantumServerComponent server)
    {
        var query = EntityQueryEnumerator<QuantumServerComponent>();
        while (query.MoveNext(out var foundUid, out var foundServer))
        {
            if (foundServer.DomainMapUid != mapUid)
                continue;

            serverUid = foundUid;
            server = foundServer;
            return true;
        }

        serverUid = default;
        server = default!;
        return false;
    }

    private string GetObjectiveInstructions(QuantumServerComponent server)
    {
        var target = server.ObjectiveGoal.ToString();
        return server.ObjectiveType switch
        {
            BitrunningObjectiveType.None => Loc.GetString("bitrunning-training-instructions-none"),
            BitrunningObjectiveType.CollectEncryptedCaches => Loc.GetString("bitrunning-training-instructions-collect", ("target", target)),
            BitrunningObjectiveType.DeliveryCacheCrate => Loc.GetString("bitrunning-training-instructions-delivery", ("target", target)),
            BitrunningObjectiveType.EliminateEnemies => Loc.GetString("bitrunning-training-instructions-eliminate", ("target", target)),
            BitrunningObjectiveType.CatchFish => Loc.GetString("bitrunning-training-instructions-catch-fish", ("target", target)),
            BitrunningObjectiveType.FillStomach => Loc.GetString("bitrunning-training-instructions-fill-stomach", ("target", target)),
            BitrunningObjectiveType.OverhydrateStomach => Loc.GetString("bitrunning-training-instructions-overhydrate-stomach", ("target", target)),
            _ => Loc.GetString("bitrunning-training-instructions-none"),
        };
    }

    private void CompleteObjective(Entity<QuantumServerComponent> serverEnt)
    {
        if (serverEnt.Comp.ObjectiveCompleted)
            return;

        serverEnt.Comp.ObjectiveCompleted = true;
        var rewardMultiplier = CalculateBaseRewardMultiplier(serverEnt.Comp);

        if (ShouldSpawnCompletionRewardCache(serverEnt.Comp) && serverEnt.Comp.ObjectiveType != BitrunningObjectiveType.DeliveryCacheCrate)
        {
            if (serverEnt.Comp is { HasExplicitCacheMarker: true, CacheCoordinates: { } markerCoordinates })
                SpawnRewardCache(serverEnt.Comp, markerCoordinates);
            else
            {
                var spawnedNearAvatar = false;
                foreach (var avatar in serverEnt.Comp.ActiveConnections)
                {
                    if (!TryComp(avatar, out TransformComponent? xform))
                        continue;

                    SpawnRewardCache(serverEnt.Comp, xform.Coordinates);
                    spawnedNearAvatar = true;
                    break;
                }

                if (!spawnedNearAvatar && serverEnt.Comp.CacheCoordinates is { } fallbackCoordinates)
                    SpawnRewardCache(serverEnt.Comp, fallbackCoordinates);
            }
        }

        var baseServerReward = 0;
        var baseBitrunningReward = 0;
        var randomServerBonus = 0;
        var randomBitrunningBonus = 0;
        if (TryGetCurrentDomain(serverEnt.Comp, out var domain) && domain != null)
        {
            baseServerReward = domain.ServerRewardPoints;
            baseBitrunningReward = domain.BitrunningRewardPoints;
            randomServerBonus = domain.RandomServerBonusPoints;
            randomBitrunningBonus = domain.RandomBitrunningBonusPoints;
        }

        // Reward multipliers intentionally share the same base scaling rules.
        var serverReward = Math.Max(0, (int) MathF.Round(baseServerReward * rewardMultiplier));
        var bitrunningReward = Math.Max(0, (int) MathF.Round(baseBitrunningReward * rewardMultiplier));

        if (serverEnt.Comp.WasRandomizedRun)
        {
            serverReward += randomServerBonus;
            bitrunningReward += randomBitrunningBonus;
        }

        if (serverReward > 0)
            serverEnt.Comp.Points += serverReward;

        AwardParticipants(serverEnt.Comp, (uint) bitrunningReward);

        var objectiveCompletedText = Loc.GetString("bitrunning-objective-completed-rewards",
            ("server", serverReward),
            ("np", bitrunningReward));

        foreach (var avatar in serverEnt.Comp.ActiveConnections)
        {
            if (!Exists(avatar))
                continue;

            _popup.PopupEntity(objectiveCompletedText, avatar, avatar, PopupType.LargeCaution);
        }

        _audio.PlayPvs(serverEnt.Comp.ObjectiveRewardSound, serverEnt.Owner);

        if (ShouldAutoStopOnObjectiveComplete(serverEnt.Comp))
            StopDomain(serverEnt);

        Dirty(serverEnt);
    }

    private void AwardParticipants(QuantumServerComponent server, uint reward)
    {
        if (reward == 0)
            return;

        HashSet<EntityUid> rewarded = [];

        foreach (var avatarUid in server.ActiveConnections)
        {
            if (!TryComp<AvatarConnectionComponent>(avatarUid, out var connection))
                continue;

            if (connection.OriginalBody is not { } bodyUid)
                continue;

            if (!rewarded.Add(bodyUid))
                continue;

            if (_bitrunningPoints.GetPointComp(bodyUid) is not { } account)
                continue;

            _bitrunningPoints.AddPoints(account, reward);
        }
    }

    private bool TryGetCurrentDomain(QuantumServerComponent server, out BitrunningVirtualDomainPrototype? domain)
    {
        domain = null;
        return server.CurrentDomain != null &&
               _domains.TryGetDomain(server.CurrentDomain, out domain);
    }

    private bool ShouldAutoStopOnObjectiveComplete(QuantumServerComponent server)
    {
        if (server.CurrentDomain == null || !_domains.TryGetDomain(server.CurrentDomain, out var domain))
            return false;

        return domain.AutoStopOnObjectiveComplete;
    }

    private void PlayAvatarLocalSound(EntityUid avatarUid, SoundSpecifier sound)
    {
        if (!TryComp<AvatarConnectionComponent>(avatarUid, out var connection) || connection.OriginalBody is not { } bodyUid)
            return;

        PlayLocalSound(bodyUid, sound);
    }

    private void PlayLocalSound(EntityUid listenerUid, SoundSpecifier sound)
    {
        if (!TryComp<ActorComponent>(listenerUid, out var actor))
            return;

        _audio.PlayEntity(sound, actor.PlayerSession, listenerUid);
    }

    private bool ShouldSpawnCompletionRewardCache(QuantumServerComponent server)
    {
        if (server.CurrentDomain == null || !_domains.TryGetDomain(server.CurrentDomain, out var domain))
            return true;

        return domain.SpawnRewardCacheOnObjectiveComplete;
    }

    private static bool HasActiveObjective(QuantumServerComponent server)
    {
        return server.ObjectiveType != BitrunningObjectiveType.None && server.ObjectiveGoal > 0;
    }

    private float CalculateBaseRewardMultiplier(QuantumServerComponent server)
    {
        var noHitCount = 0;
        foreach (var uid in server.ActiveConnections)
        {
            if (CompOrNull<AvatarConnectionComponent>(uid)?.NoHit == true)
                noHitCount++;
        }

        var total = 1f;
        total += server.QualityBonus;
        total += Math.Max(0, server.ActiveConnections.Count - 1) * 0.5f;
        total += noHitCount * 0.4f;
        total += server.ThreatsSpawned * 0.5f;
        return Math.Max(1f, total);
    }

    private void SpawnRewardCache(QuantumServerComponent server, EntityCoordinates coordinates)
    {
        var cache = Spawn(server.CompletionRewardCachePrototype, coordinates);
        _byteforge.TryFillRewardCacheWithLoot(cache, server);
        _sparks.DoSparks(coordinates);
    }

    private void ApplyBitrunningExitEffects(EntityUid? originalBody, EntityUid? serverUid)
    {
        if (originalBody is not { } bodyUid || !Exists(bodyUid))
            return;

        if (serverUid is not { } currentServerUid || !TryComp<QuantumServerComponent>(currentServerUid, out var currentServer))
            return;

        _stun.TryAddParalyzeDuration(bodyUid, currentServer.ExitParalyzeTime);
        _statusEffects.TryUpdateStatusEffectDuration(bodyUid, ExitBlindnessStatusEffect, currentServer.ExitBlindnessTime);
    }

    [SubscribeLocalEvent]
    private void OnAvatarDamaged(Entity<AvatarConnectionComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null || !args.DamageIncreased)
            return;

        ent.Comp.NoHit = false;
        ent.Comp.DisconnectBlockedUntil = _timing.CurTime + DisconnectActionBlockDuration;

        if (ent.Comp.DisconnectActionEntity is { } disconnectAction)
            _actions.SetCooldown(disconnectAction, _timing.CurTime, ent.Comp.DisconnectBlockedUntil);
    }

    [SubscribeLocalEvent]
    private void OnAvatarStateChanged(Entity<AvatarConnectionComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        DisconnectAvatar(ent, true);
    }

    private bool IsAvatarInCriticalState(EntityUid avatarUid)
    {
        return TryComp<MobStateComponent>(avatarUid, out var mobState)
               && mobState.CurrentState is MobState.SoftCrit or MobState.Critical;
    }

    private void TransferAvatarDamageToBitrunner(Entity<AvatarConnectionComponent> avatar, EntityUid bodyUid, bool fatal)
    {
        if (!TryComp<DamageableComponent>(avatar, out var avatarDamage))
            return;

        var serverDamageScale = 0.20f;
        if (avatar.Comp.Server is { } serverUid && TryComp<QuantumServerComponent>(serverUid, out var server))
            serverDamageScale = server.BaseExitDamageScale * server.FinalExitDamageMultiplier;

        var scaledDamage = _damageable.GetTotalDamage((avatar, avatarDamage)) > 0
            ? _damageable.GetAllDamage((avatar, avatarDamage)) * serverDamageScale
            : new DamageSpecifier
            {
                DamageDict =
                {
                    ["Blunt"] = fatal
                        ? 20f
                        : 10f,
                    ["Cellular"] = fatal // No brain damage, lol 🥹
                        ? 2f
                        : 1f,
                },
            };

        _damageable.TryChangeDamage(bodyUid, scaledDamage, ignoreResistances: true);
    }

    private void KillAvatar(EntityUid avatarUid)
    {
        if (_mobState.IsDead(avatarUid))
            return;

        _mobState.ChangeMobState(avatarUid, MobState.Dead);
        _deathgasp.Deathgasp(avatarUid);
    }

    [SubscribeLocalEvent]
    private void OnAvatarDisconnectAction(Entity<AvatarConnectionComponent> ent, ref BitrunningDisconnectAvatarActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer == EntityUid.Invalid ? ent.Owner : args.Performer;
        args.Handled = TryRequestDisconnectAvatar(ent.Owner, user, true);
    }

    [SubscribeLocalEvent]
    private void OnAvatarDisconnectDoAfter(Entity<AvatarConnectionComponent> ent, ref BitrunningDisconnectAvatarDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.DisconnectBlockedUntil > _timing.CurTime)
            return;

        DisconnectAvatar(ent.Owner, false);
        args.Handled = true;
    }

    public bool TryRequestDisconnectAvatar(EntityUid avatarUid, EntityUid userUid, bool showPopup)
    {
        if (!TryComp<AvatarConnectionComponent>(avatarUid, out var connection))
            return false;

        var now = _timing.CurTime;
        if (connection.DisconnectBlockedUntil > now)
        {
            if (showPopup)
            {
                var remainingSeconds = Math.Max(1, (int) Math.Ceiling((connection.DisconnectBlockedUntil - now).TotalSeconds));
                _popup.PopupEntity(Loc.GetString("bitrunning-avatar-disconnect-blocked", ("seconds", remainingSeconds)), avatarUid, userUid);
            }

            return true;
        }

        var doAfter = new DoAfterArgs(EntityManager, userUid, DisconnectActionDoAfterDuration, new BitrunningDisconnectAvatarDoAfterEvent(), avatarUid, target: avatarUid)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            CancelDuplicate = true,
            NeedHand = false,
            MovementThreshold = 1.0f,
        };

        return _doAfter.TryStartDoAfter(doAfter);
    }

    [SubscribeLocalEvent]
    private void OnGhostAttemptForAvatar(GhostAttemptHandleEvent ev)
    {
        if (ev.Handled)
            return;

        var currentEntity = ev.Mind.CurrentEntity;
        if (currentEntity == null || !HasComp<AvatarConnectionComponent>(currentEntity.Value))
            return;

        ev.Handled = true;
        ev.Result = true;
        DisconnectAvatar(currentEntity.Value, true);
    }

    [SubscribeLocalEvent]
    private void OnAvatarTerminating(Entity<AvatarConnectionComponent> ent, ref EntityTerminatingEvent args)
    {
        // Protection if avatar un-polymorphing
        if (HasComp<PolymorphedEntityComponent>(ent))
            return;

        if (ent.Comp.Server is { } serverUid && TryComp<QuantumServerComponent>(serverUid, out var server))
        {
            server.ActiveConnections.Remove(ent.Owner);
            Dirty(serverUid, server);
        }

        if (ent.Comp.Netpod is { } podUid && TryComp<NetpodComponent>(podUid, out var pod))
        {
            if (pod.Avatar == ent.Owner)
                pod.Avatar = null;

            EnsureComp<AvatarNavRelayComponent>(podUid).RelayEntity = null;
            _surveillanceCamera.RefreshViewerTargets(podUid); // Nebula edit

            pod.Occupant = TryComp<NetpodContainerComponent>(podUid, out var containerComp)
                ? containerComp.BodyContainer.ContainedEntity
                : null;

            Dirty(podUid, pod);
            _netpod.UpdateVisuals((podUid, pod));
        }

        if (ent.Comp.OriginalBody is not { } bodyUid || TerminatingOrDeleted(bodyUid))
            return;

        if (CanRedirectToBitrunnerBody(ent.Comp, bodyUid))
            TransferAvatarDamageToBitrunner(ent, bodyUid, true);

        if (!TryResolveRunnerMind(ent, out var mindId))
            return;

        _mind.TransferTo(mindId, bodyUid);
    }

    [SubscribeLocalEvent]
    private void OnAvatarSuicide(Entity<AvatarConnectionComponent> ent, ref SuicideEvent args)
    {
        if (args.Handled)
            return;

        DisconnectAvatar(ent, true);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnAvatarPolymorphed(Entity<AvatarConnectionComponent> ent, ref PolymorphedEvent args)
    {
        if (args.OldEntity != ent.Owner)
            return;

        var newAvatarUid = args.NewEntity;
        if (newAvatarUid == EntityUid.Invalid || Deleted(newAvatarUid))
            return;

        var newConnection = EnsureComp<AvatarConnectionComponent>(newAvatarUid);
        newConnection.OriginalBody = ent.Comp.OriginalBody;
        newConnection.Server = ent.Comp.Server;
        newConnection.Netpod = ent.Comp.Netpod;
        newConnection.RunnerMind = ent.Comp.RunnerMind;
        newConnection.NoHit = ent.Comp.NoHit;
        newConnection.DisconnectActionPrototype = ent.Comp.DisconnectActionPrototype;
        newConnection.DeleteOnDisconnect = ent.Comp.DeleteOnDisconnect;
        newConnection.DisconnectBlockedUntil = ent.Comp.DisconnectBlockedUntil;

        _actions.RemoveAction(ent.Comp.DisconnectActionEntity);
        ent.Comp.DisconnectActionEntity = null;
        _actions.AddAction(newAvatarUid, ref newConnection.DisconnectActionEntity, newConnection.DisconnectActionPrototype, newAvatarUid);
        StripNonAvatarPolymorphComponents(newAvatarUid);
        EnsureComp<AvatarNavRelayComponent>(newAvatarUid).RelayEntity = newConnection.Netpod;

        if (newConnection.Server is { } serverUid && TryComp<QuantumServerComponent>(serverUid, out var server))
        {
            server.ActiveConnections.Remove(ent.Owner);
            server.ActiveConnections.Add(newAvatarUid);
            Dirty(serverUid, server);
        }

        if (newConnection.Netpod is { } podUid && TryComp<NetpodComponent>(podUid, out var pod))
        {
            if (pod.Avatar == ent.Owner)
                pod.Avatar = newAvatarUid;

            EnsureComp<AvatarNavRelayComponent>(podUid).RelayEntity = newAvatarUid;
            _surveillanceCamera.RefreshViewerTargets(podUid); // Nebula edit

            Dirty(podUid, pod);
            _netpod.UpdateVisuals((podUid, pod));
        }

        RemCompDeferred<AvatarConnectionComponent>(ent);
        _bitrunningDisk.RefreshAvatarEffects(newAvatarUid);
    }


    [SubscribeLocalEvent]
    private static void OnAvatarSpoke(Entity<AvatarConnectionComponent> ent, ref EntitySpokeEvent args)
    {
        if (args.Channel != null)
            args.Channel = null;
    }

    private EntityUid SpawnAvatarForRunner(QuantumServerComponent server, EntityUid user, EntityCoordinates coordinates)
    {
        var avatar = !ShouldLoadProfileAvatar(server, user) || !TryGetHumanoidProfile(user, out var profile)
            ? Spawn(server.AvatarPrototype, coordinates)
            : _spawning.SpawnPlayerMob(coordinates, null, profile, null); // Nebula edit

        EnsureComp<AntagImmuneComponent>(avatar);
        return avatar;
    }

    private bool TryGetHumanoidProfile(EntityUid user, out HumanoidCharacterProfile profile)
    {
        profile = default!;

        if (!TryComp<ActorComponent>(user, out var actor))
            return false;

        if (_preferences.GetPreferences(actor.PlayerSession.UserId).SelectedCharacter is not HumanoidCharacterProfile selected)
            return false;

        profile = selected;
        return true;
    }

    private bool ShouldLoadProfileAvatar(QuantumServerComponent server, EntityUid user)
    {
        return server.AllowProfileLoad && HasCompInContainerTree<BitrunningProfileDiskComponent>(user);
    }

    private bool HasCompInContainerTree<T>(EntityUid root) where T : Component
    {
        Queue<EntityUid> queue = [];
        HashSet<EntityUid> visited = [];
        queue.Enqueue(root);

        while (queue.TryDequeue(out var current))
        {
            if (!visited.Add(current))
                continue;

            if (HasComp<T>(current))
                return true;

            if (!TryComp<ContainerManagerComponent>(current, out var manager))
                continue;

            foreach (var container in manager.Containers.Values)
            {
                foreach (var contained in container.ContainedEntities)
                {
                    queue.Enqueue(contained);
                }
            }
        }

        return false;
    }

    private void StripNonAvatarPolymorphComponents(EntityUid avatar)
    {
        RemComp<GhostRoleComponent>(avatar);
        RemComp<GhostTakeoverAvailableComponent>(avatar);
        RemComp<HTNComponent>(avatar);
        RemComp<FaunaComponent>(avatar);
    }

    private bool TryResolveRunnerMind(Entity<AvatarConnectionComponent> avatar, out EntityUid mindId)
    {
        if (avatar.Comp.RunnerMind is { } storedMind && Exists(storedMind))
        {
            mindId = storedMind;
            return true;
        }

        if (TryComp<MindContainerComponent>(avatar, out var container) && container.Mind is { } avatarMind)
        {
            mindId = avatarMind;
            return true;
        }

        mindId = default;
        return false;
    }

    private bool CanRedirectToBitrunnerBody(AvatarConnectionComponent connection, EntityUid? originalBody)
    {
        if (originalBody is not { } bodyUid || connection.Netpod is not { } podUid)
            return false;

        if (!TryComp<NetpodContainerComponent>(podUid, out var containerComp))
            return false;

        return containerComp.BodyContainer.ContainedEntity == bodyUid;
    }

    public void SetBroadcastState(EntityUid serverUid, bool enabled)
    {
        if (!TryComp<QuantumServerComponent>(serverUid, out var server))
            return;

        server.BroadcastEnabled = enabled;
        Dirty(serverUid, server);

        foreach (var avatar in server.ActiveConnections)
        {
            if (!TryComp<AvatarConnectionComponent>(avatar, out var connection))
                continue;

            SetAvatarBroadcastEnabled((avatar, connection), server, enabled);
        }
    }

    public string? GetRandomDomainId(EntityUid serverUid)
    {
        if (!TryComp<QuantumServerComponent>(serverUid, out var server))
            return null;

        var emagged = HasComp<EmaggedComponent>(serverUid);
        var allowed = _domains.GetAllDomains()
            .Where(d => d.Cost <= server.Points)
            .Where(d => CanAccessDomain(server, d))
            .Where(d => emagged || d.Difficulty != BitrunningDifficulty.Extreme)
            .Select(d => d.ID)
            .ToList();

        return allowed.Count == 0
            ? null
            : _random.Pick(allowed);
    }

    private static bool CanAccessDomain(QuantumServerComponent server, BitrunningVirtualDomainPrototype domain)
    {
        if (!domain.HiddenUntilScanned)
            return true;

        return server.ScannerTier >= domain.RequiredScannerTier;
    }

    private BitrunningObjectiveType PickObjectiveType(BitrunningVirtualDomainPrototype domain)
    {
        if (domain.ObjectiveTypePool.Length == 0)
            return domain.ObjectiveType;

        return _random.Pick(domain.ObjectiveTypePool);
    }

    private int ResolveObjectiveGoal(Entity<QuantumServerComponent> serverEnt, BitrunningVirtualDomainPrototype domain, BitrunningObjectiveType objectiveType)
    {
        var explicitTarget = domain.ObjectiveTargetByType.TryGetValue(objectiveType, out var byTypeTarget)
            ? Math.Max(byTypeTarget, 0)
            : Math.Max(domain.ObjectiveTarget, 0);
        if (explicitTarget > 0 || objectiveType != BitrunningObjectiveType.EliminateEnemies)
            return explicitTarget;

        if (serverEnt.Comp.DomainMapUid is not { } mapUid)
            return 0;

        var target = 0;
        var enemies = EntityQueryEnumerator<BitrunningDomainEnemyObjectiveComponent, TransformComponent>();
        while (enemies.MoveNext(out _, out _, out var xform))
        {
            if (xform.MapUid != mapUid)
                continue;

            target++;
        }

        return target;
    }

    private void CleanupObjectiveArtifacts(Entity<QuantumServerComponent> serverEnt)
    {
        if (serverEnt.Comp.DomainMapUid is not { } mapUid)
            return;

        var keepEncryptedCaches = serverEnt.Comp.ObjectiveType == BitrunningObjectiveType.CollectEncryptedCaches;
        var keepDeliveryCaches = serverEnt.Comp.ObjectiveType == BitrunningObjectiveType.DeliveryCacheCrate;

        var objectivePoints = EntityQueryEnumerator<BitrunningObjectivePointComponent, TransformComponent>();
        while (objectivePoints.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapUid != mapUid || keepEncryptedCaches)
                continue;

            QueueDel(uid);
        }

        var deliveryObjectives = EntityQueryEnumerator<BitrunningObjectiveCargoComponent, TransformComponent>();
        while (deliveryObjectives.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapUid != mapUid || keepDeliveryCaches)
                continue;

            QueueDel(uid);
        }
    }

    private void ReleaseAvatarHands(EntityUid avatarUid)
    {
        foreach (var hand in _hands.EnumerateHands(avatarUid).ToArray())
        {
            if (!_hands.TryGetHeldItem(avatarUid, hand, out var held))
                continue;

            if (TryComp<VirtualItemComponent>(held, out _))
            {
                QueueDel(held.Value);
                continue;
            }

            _hands.TryDrop(avatarUid, hand);
        }
    }
}
