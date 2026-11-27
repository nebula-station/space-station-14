// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using Content.Server.Chat.Systems;
using Content.Shared.Station.Systems;
using Content.Server.SurveillanceCamera;
using Content.Shared._Nebula.Bodycam;
using Content.Shared._Nebula.CCVar;
using Content.Shared.Access.Systems;
using Content.Shared.Chat;
using Content.Shared.Emp;
using Content.Shared.Examine;
using Content.Shared.Eye;
using Content.Shared.GameTicking;
using Content.Shared.Storage;
using Content.Shared.SurveillanceCamera.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Trauma.Common.Language.Systems;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Audio.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Nebula.Bodycam;

public sealed partial class BodycamRecord
{
    public required int Id;
    public required NetEntity Camera;
    public required string Name;
    public readonly List<BodycamRecordSegment> Segments = new();
}

public sealed partial class BodycamRecordSegment
{
    public required GameTick Start;
    public GameTick? End;
    public required string Wearer;
}

public sealed partial class BodycamSystem : SharedBodycamSystem
{
    [Dependency] private IReplayRecordingManager _replay = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SurveillanceCameraSystem _camera = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private CommonLanguageSystem _language = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private ExamineSystemShared _examine = default!;

    private static readonly FieldInfo? GlobalOverrideField =
        typeof(PvsOverrideSystem).GetField("GlobalOverride", BindingFlags.Instance | BindingFlags.NonPublic);

    private sealed class ActiveCamera
    {
        public HashSet<NetEntity> View = new();
        public TimeSpan NextSample;
        public bool NeedsReset = true;
    }

    private readonly Dictionary<EntityUid, ActiveCamera> _active = new();
    private readonly Dictionary<NetEntity, BodycamRecord> _records = new();
    private readonly List<EntityUid> _pendingSpawns = new();
    private readonly HashSet<EntityUid> _lookupSet = new();
    private readonly HashSet<EntityUid> _viewSet = new();
    private readonly HashSet<EntityUid> _traversed = new();
    private List<Entity<MapGridComponent>> _grids = new();
    private EntityQuery<VisibilityComponent> _visibilityQuery;
    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<MapComponent> _mapQuery;
    private EntityQuery<MobStateComponent> _mobQuery;
    private EntityQuery<StorageComponent> _storageQuery;
    private EntityQuery<AudioComponent> _audioQuery;
    private int _nextId = 1;
    private bool _wasRecording;
    private float _range;
    private TimeSpan _interval;

    public IReadOnlyDictionary<NetEntity, BodycamRecord> Records => _records;

    public override void Initialize()
    {
        base.Initialize();

        _visibilityQuery = GetEntityQuery<VisibilityComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _mapQuery = GetEntityQuery<MapComponent>();
        _mobQuery = GetEntityQuery<MobStateComponent>();
        _storageQuery = GetEntityQuery<StorageComponent>();
        _audioQuery = GetEntityQuery<AudioComponent>();

        SubscribeLocalEvent<BodycamComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BodycamComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BodycamComponent, BodycamToggledEvent>(OnToggled);
        SubscribeLocalEvent<BodycamComponent, EmpPulseEvent>(OnEmpPulse, after: [typeof(SurveillanceCameraSystem)]);
        SubscribeLocalEvent<BodycamComponent, EmpDisabledRemovedEvent>(OnEmpDisabledRemoved, after: [typeof(SurveillanceCameraSystem)]);
        SubscribeLocalEvent<BodycamComponent, RadioReceiveEvent>(OnRadioReceive);
        SubscribeLocalEvent<BodycamSpeechEvent>(OnSpeech);
        SubscribeLocalEvent<BodycamEmoteEvent>(OnEmote);
        SubscribeLocalEvent<BodycamAnnouncementEvent>(OnAnnouncement);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        EntityManager.EntityInitialized += OnEntityInitialized;

        Subs.CVar(_cfg, NCCVars.BodycamViewRange, v => _range = v, true);
        Subs.CVar(_cfg, NCCVars.BodycamSampleInterval, v => _interval = TimeSpan.FromSeconds(v), true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        EntityManager.EntityInitialized -= OnEntityInitialized;
    }

    public bool TryGetRecord(int id, out BodycamRecord record)
    {
        foreach (var candidate in _records.Values)
        {
            if (candidate.Id != id)
                continue;

            record = candidate;
            return true;
        }

        record = default!;
        return false;
    }

    public bool IsActive(NetEntity camera)
    {
        return TryGetEntity(camera, out var uid) && _active.ContainsKey(uid.Value);
    }

    private void OnMapInit(Entity<BodycamComponent> ent, ref MapInitEvent args)
    {
        if (TryComp<SurveillanceCameraComponent>(ent, out var camera))
            _camera.SetActive(ent, ent.Comp.Enabled, camera);
        SyncRecording(ent);
    }

    private void OnShutdown(Entity<BodycamComponent> ent, ref ComponentShutdown args)
    {
        Deactivate(ent);
    }

    private void OnToggled(Entity<BodycamComponent> ent, ref BodycamToggledEvent args)
    {
        if (TryComp<SurveillanceCameraComponent>(ent, out var camera))
            _camera.SetActive(ent, args.Enabled, camera);
        SyncRecording(ent);
    }

    private void OnEmpPulse(Entity<BodycamComponent> ent, ref EmpPulseEvent args)
    {
        SyncRecording(ent);
    }

    private void OnEmpDisabledRemoved(Entity<BodycamComponent> ent, ref EmpDisabledRemovedEvent args)
    {
        if (!ent.Comp.Enabled && TryComp<SurveillanceCameraComponent>(ent, out var camera) && camera.Active)
            _camera.SetActive(ent, false, camera);
        SyncRecording(ent);
    }

    private void SyncRecording(Entity<BodycamComponent> ent)
    {
        if (ent.Comp.Enabled && (!TryComp<SurveillanceCameraComponent>(ent, out var camera) || camera.Active))
            Activate(ent);
        else
            Deactivate(ent);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _records.Clear();
        _nextId = 1;
        foreach (var camera in _active.Values)
            camera.NeedsReset = true;
    }

    private void Activate(EntityUid uid)
    {
        if (_active.ContainsKey(uid))
            return;

        _active[uid] = new ActiveCamera { NextSample = _timing.CurTime };
        var record = EnsureRecord(uid);
        OpenSegment(record, uid);
        _replay.RecordServerMessage(new BodycamReplayStatusEvent(GetNetEntity(uid), true));
    }

    private void Deactivate(EntityUid uid)
    {
        if (!_active.Remove(uid))
            return;

        if (_records.TryGetValue(GetNetEntity(uid), out var record))
            CloseSegment(record);

        _replay.RecordServerMessage(new BodycamReplayStatusEvent(GetNetEntity(uid), false));
    }

    private BodycamRecord EnsureRecord(EntityUid uid)
    {
        var net = GetNetEntity(uid);
        if (_records.TryGetValue(net, out var record))
            return record;

        var id = _nextId++;
        record = new BodycamRecord
        {
            Id = id,
            Camera = net,
            Name = Loc.GetString("bodycam-record-name", ("id", id), ("name", Name(uid))),
        };
        _records[net] = record;
        return record;
    }

    private void OpenSegment(BodycamRecord record, EntityUid uid)
    {
        CloseSegment(record);
        record.Segments.Add(new BodycamRecordSegment
        {
            Start = _timing.CurTick,
            Wearer = DescribeWearer(GetWearer(uid)),
        });
    }

    private void CloseSegment(BodycamRecord record)
    {
        if (record.Segments.Count > 0 && record.Segments[^1].End == null)
            record.Segments[^1].End = _timing.CurTick;
    }

    private EntityUid? GetWearer(EntityUid camera)
    {
        var parent = Transform(camera).ParentUid;
        while (parent.IsValid() && !_gridQuery.HasComp(parent) && !_mapQuery.HasComp(parent))
        {
            if (_mobQuery.HasComp(parent))
                return parent;

            parent = Transform(parent).ParentUid;
        }

        return null;
    }

    private string DescribeWearer(EntityUid? wearer)
    {
        if (wearer == null)
            return Loc.GetString("bodycam-wearer-none");

        if (_idCard.TryFindIdCard(wearer.Value, out var id) && id.Comp.FullName is { } fullName)
        {
            return string.IsNullOrEmpty(id.Comp.LocalizedJobTitle)
                ? fullName
                : Loc.GetString("bodycam-wearer-job", ("name", fullName), ("job", id.Comp.LocalizedJobTitle));
        }

        return Identity.Name(wearer.Value, EntityManager);
    }

    private void OnEntityInitialized(Entity<MetaDataComponent> ent)
    {
        if (_active.Count > 0)
            _pendingSpawns.Add(ent.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var recording = _replay.IsRecording;
        if (recording && !_wasRecording)
        {
            foreach (var (uid, camera) in _active)
            {
                camera.NeedsReset = true;
                _replay.RecordServerMessage(new BodycamReplayStatusEvent(GetNetEntity(uid), true));
            }
        }

        _wasRecording = recording;
        if (!recording || _active.Count == 0)
        {
            _pendingSpawns.Clear();
            return;
        }

        var now = _timing.CurTime;
        foreach (var (uid, camera) in _active)
        {
            if (camera.NextSample > now && !camera.NeedsReset)
                continue;

            camera.NextSample = now + _interval;
            Sample(uid, camera);
        }

        if (_pendingSpawns.Count > 0)
            ProcessSpawns();
    }

    private void Sample(EntityUid uid, ActiveCamera camera)
    {
        if (TerminatingOrDeleted(uid))
            return;

        BuildView(uid, _viewSet);

        var next = new HashSet<NetEntity>(_viewSet.Count);
        foreach (var entity in _viewSet)
            next.Add(GetNetEntity(entity));

        NetEntity[] entered;
        NetEntity[] left;
        var reset = camera.NeedsReset;
        if (reset)
        {
            entered = next.ToArray();
            left = Array.Empty<NetEntity>();
        }
        else
        {
            entered = next.Where(e => !camera.View.Contains(e)).ToArray();
            left = camera.View.Where(e => !next.Contains(e)).ToArray();
        }

        camera.View = next;
        camera.NeedsReset = false;

        _replay.RecordServerMessage(new BodycamReplayViewEvent(
            GetNetEntity(uid),
            GetNetCoordinates(_transform.GetMoverCoordinates(uid)),
            _range,
            reset,
            entered,
            left));

        if (_records.TryGetValue(GetNetEntity(uid), out var record) && record.Segments.Count > 0)
        {
            var wearer = DescribeWearer(GetWearer(uid));
            if (record.Segments[^1].End == null && record.Segments[^1].Wearer != wearer)
                OpenSegment(record, uid);
        }
    }

    private void ProcessSpawns()
    {
        foreach (var (uid, camera) in _active)
        {
            var cameraPos = _transform.GetMapCoordinates(uid);
            List<NetEntity>? entered = null;

            foreach (var spawned in _pendingSpawns)
            {
                if (TerminatingOrDeleted(spawned))
                    continue;

                var pos = _transform.GetMapCoordinates(spawned);
                var range = _range;
                if (_audioQuery.TryComp(spawned, out var audio))
                {
                    if (audio.Global || !AudibleTo(audio, uid))
                        continue;

                    range = MathF.Max(range, audio.Params.MaxDistance);
                }

                if (pos.MapId != cameraPos.MapId || (pos.Position - cameraPos.Position).Length() > range)
                    continue;

                if (!IsObservable(spawned))
                    continue;

                _viewSet.Clear();
                AddWithAncestors(spawned, _viewSet);
                foreach (var entity in _viewSet)
                {
                    var net = GetNetEntity(entity);
                    if (camera.View.Add(net))
                        (entered ??= new()).Add(net);
                }
            }

            if (entered != null)
            {
                _replay.RecordServerMessage(new BodycamReplayViewEvent(
                    GetNetEntity(uid),
                    GetNetCoordinates(_transform.GetMoverCoordinates(uid)),
                    _range,
                    false,
                    entered.ToArray(),
                    Array.Empty<NetEntity>()));
            }
        }

        _pendingSpawns.Clear();
    }

    private void BuildView(EntityUid camera, HashSet<EntityUid> result)
    {
        result.Clear();
        _traversed.Clear();
        AddWithAncestors(camera, result);

        var mapCoords = _transform.GetMapCoordinates(camera);
        if (mapCoords.MapId == MapId.Nullspace)
            return;

        if (_map.TryGetMap(mapCoords.MapId, out var mapUid))
            result.Add(mapUid.Value);

        _grids.Clear();
        var box = Box2.CenteredAround(mapCoords.Position, new Vector2(_range * 2));
        _map.FindGridsIntersecting(mapCoords.MapId, box, ref _grids);
        foreach (var grid in _grids)
            AddWithAncestors(grid.Owner, result);

        _lookupSet.Clear();
        _lookup.GetEntitiesInRange(mapCoords.MapId, mapCoords.Position, _range, _lookupSet, LookupFlags.Uncontained | LookupFlags.Approximate);
        foreach (var entity in _lookupSet)
        {
            if (IsObservable(entity))
                AddTree(entity, result);
        }

        if (GlobalOverrideField?.GetValue(_pvsOverride) is HashSet<EntityUid> globals)
        {
            foreach (var entity in globals)
            {
                if (!TerminatingOrDeleted(entity) && IsObservable(entity))
                    AddTree(entity, result);
            }
        }

        var audio = EntityQueryEnumerator<AudioComponent, TransformComponent>();
        while (audio.MoveNext(out var uid, out var comp, out var xform))
        {
            if (comp.Global || xform.MapID != mapCoords.MapId)
                continue;

            var distance = (_transform.GetWorldPosition(xform) - mapCoords.Position).Length();
            if (distance <= MathF.Max(_range, comp.Params.MaxDistance) && AudibleTo(comp, camera) && IsObservable(uid))
                AddWithAncestors(uid, result);
        }
    }

    private void AddTree(EntityUid uid, HashSet<EntityUid> result)
    {
        if (!_traversed.Add(uid))
            return;

        AddWithAncestors(uid, result);

        var enumerator = Transform(uid).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            if (_container.TryGetContainingContainer((child, null, null), out var container) && !IsShown(container))
                continue;

            if (IsVisible(child))
                AddTree(child, result);
        }
    }

    private bool AddWithAncestors(EntityUid uid, HashSet<EntityUid> result)
    {
        if (!result.Add(uid))
            return false;

        var parent = Transform(uid).ParentUid;
        while (parent.IsValid() && result.Add(parent))
            parent = Transform(parent).ParentUid;

        return true;
    }

    private bool IsVisible(EntityUid uid)
    {
        return !_visibilityQuery.TryComp(uid, out var visibility)
               || (visibility.Layer & ~(int) VisibilityFlags.Normal) == 0;
    }

    private bool IsShown(BaseContainer container)
    {
        return !_storageQuery.TryComp(container.Owner, out var storage) || storage.Container != container;
    }

    private bool AudibleTo(AudioComponent audio, EntityUid camera)
    {
        if (audio.IncludedEntities == null)
            return true;

        var listener = GetWearer(camera) ?? camera;
        return Enumerable.Contains(audio.IncludedEntities, listener);
    }

    private bool IsObservable(EntityUid uid)
    {
        var current = uid;
        while (current.IsValid() && !_gridQuery.HasComp(current) && !_mapQuery.HasComp(current))
        {
            if (!IsVisible(current))
                return false;

            if (_container.TryGetContainingContainer((current, null, null), out var container) && !IsShown(container))
                return false;

            current = Transform(current).ParentUid;
        }

        return true;
    }

    private void OnSpeech(BodycamSpeechEvent ev)
    {
        if (_active.Count == 0 || !_replay.IsRecording)
            return;

        var sourcePos = _transform.GetMapCoordinates(ev.Source);
        foreach (var uid in _active.Keys)
        {
            var pos = _transform.GetMapCoordinates(uid);
            if (pos.MapId != sourcePos.MapId)
                continue;

            var distance = (pos.Position - sourcePos.Position).Length();
            if (distance > (ev.Whisper ? SharedChatSystem.WhisperMuffledRange : SharedChatSystem.VoiceRange))
                continue;

            var listener = GetWearer(uid) ?? uid;
            var understood = _language.CanUnderstand(listener, ev.Language.ID);
            var text = understood ? ev.Message : ev.ObfuscatedMessage;
            string wrapped;
            ChatChannel channel;

            if (ev.Whisper)
            {
                channel = ChatChannel.Whisper;
                if (!ev.Language.SpeechOverride.RequireLOS && distance <= SharedChatSystem.WhisperClearRange
                    || _examine.InRangeUnOccluded(ev.Source, uid, SharedChatSystem.WhisperClearRange))
                {
                    wrapped = _chat.WrapWhisperMessage(ev.Source, "chat-manager-entity-whisper-wrap-message", ev.Name, text, ev.Language, ev.Color);
                }
                else if (_examine.InRangeUnOccluded(ev.Source, uid, SharedChatSystem.WhisperMuffledRange))
                {
                    text = _chat.ObfuscateMessageReadability(text, _random);
                    wrapped = _chat.WrapWhisperMessage(ev.Source, "chat-manager-entity-whisper-wrap-message", ev.NameIdentity, text, ev.Language, ev.Color);
                }
                else
                {
                    if (ev.Language.SpeechOverride.RequireLOS)
                        continue;

                    text = _chat.ObfuscateMessageReadability(text, _random);
                    wrapped = _chat.WrapWhisperMessage(ev.Source, "chat-manager-entity-whisper-unknown-wrap-message", string.Empty, text, ev.Language, ev.Color);
                }
            }
            else
            {
                if ((ChatSystem.SpeakRespectsLOS || ev.Language.SpeechOverride.RequireLOS)
                    && !_examine.InRangeUnOccluded(ev.Source, uid, SharedChatSystem.VoiceRange))
                {
                    continue;
                }

                channel = ChatChannel.Local;
                wrapped = _chat.WrapPublicMessage(ev.Source, ev.Name, text, _chat.GetSpeechVerb(ev.Source, text), ev.Language, ev.Color);
            }

            var message = new ChatMessage(channel, text, wrapped, GetNetEntity(ev.Source), null, ev.Range == ChatTransmitRange.HideChat);
            _replay.RecordServerMessage(new BodycamReplayChatEvent(GetNetEntity(uid), message));
        }
    }

    private void OnEmote(BodycamEmoteEvent ev)
    {
        if (_active.Count == 0 || !_replay.IsRecording)
            return;

        var sourcePos = _transform.GetMapCoordinates(ev.Source);
        foreach (var uid in _active.Keys)
        {
            var pos = _transform.GetMapCoordinates(uid);
            if (pos.MapId != sourcePos.MapId || (pos.Position - sourcePos.Position).Length() > SharedChatSystem.VoiceRange)
                continue;

            if (ChatSystem.EmoteRespectsLOS && !_examine.InRangeUnOccluded(ev.Source, uid, SharedChatSystem.VoiceRange))
                continue;

            var message = new ChatMessage(ChatChannel.Emotes, ev.Message, ev.Wrapped, GetNetEntity(ev.Source), null, ev.Range == ChatTransmitRange.HideChat);
            _replay.RecordServerMessage(new BodycamReplayChatEvent(GetNetEntity(uid), message));
        }
    }

    private void OnRadioReceive(Entity<BodycamComponent> ent, ref RadioReceiveEvent args)
    {
        if (!_active.ContainsKey(ent) || !_replay.IsRecording)
            return;

        if (GetWearer(ent) is not { } wearer || !WearerHears(wearer, args.Channel.ID))
            return;

        var source = _language.CanUnderstand(wearer, args.Language.ID) ? args.OriginalChatMsg : args.LanguageObfuscatedChatMsg;
        var (muffled, wrapped) = MuffleRadio(source.WrappedMessage);

        var message = new ChatMessage(ChatChannel.Radio, muffled, wrapped, source.SenderEntity, null, colorOverride: source.MessageColorOverride);
        _replay.RecordServerMessage(new BodycamReplayChatEvent(GetNetEntity(ent), message));
    }

    private (string Message, string Wrapped) MuffleRadio(string wrapped)
    {
        var nodes = FormattedMessage.FromMarkupPermissive(wrapped).Nodes;
        var start = -1;
        for (var i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].Name == "font" && !nodes[i].Closing)
            {
                start = i;
                break;
            }
        }

        var result = new StringBuilder(wrapped.Length);
        var message = new StringBuilder();
        var depth = 0;
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (i == start)
            {
                depth = 1;
                result.Append(node);
                continue;
            }

            if (depth > 0 && node.Name == "font")
            {
                depth += node.Closing ? -1 : 1;
                if (depth == 0)
                {
                    result.Append(node);
                    continue;
                }
            }

            var inside = start < 0 || depth > 0;
            if (node.IsPlainText && inside)
            {
                var muffled = new MarkupNode(_chat.ObfuscateMessageReadability(node.Value.StringValue ?? string.Empty, _random));
                result.Append(muffled);
                message.Append(muffled);
                continue;
            }

            result.Append(node);
            if (depth > 0)
                message.Append(node);
        }

        return (message.ToString(), result.ToString());
    }

    private bool WearerHears(EntityUid wearer, string channel)
    {
        if (TryComp<ActiveRadioComponent>(wearer, out var own) && (own.ReceiveAllChannels || own.Channels.Contains(channel)))
            return true;

        return _inventory.TryGetSlotEntity(wearer, "ears", out var headset)
               && TryComp<ActiveRadioComponent>(headset, out var radio)
               && (radio.ReceiveAllChannels || radio.Channels.Contains(channel));
    }

    private void OnAnnouncement(BodycamAnnouncementEvent ev)
    {
        if (_active.Count == 0 || !_replay.IsRecording)
            return;

        foreach (var uid in _active.Keys)
        {
            if (!HeardAnnouncement(uid, ev))
                continue;

            var message = new ChatMessage(ChatChannel.Radio, ev.Message, ev.Wrapped, NetEntity.Invalid, null, colorOverride: ev.Color);
            _replay.RecordServerMessage(new BodycamReplayChatEvent(GetNetEntity(uid), message));
        }
    }

    private bool HeardAnnouncement(EntityUid camera, BodycamAnnouncementEvent ev)
    {
        if (ev.Filter == null)
            return true;

        if (GetWearer(camera) is { } wearer && TryComp<ActorComponent>(wearer, out var actor))
            return ev.Filter.Recipients.Contains(actor.PlayerSession);

        return ev.Station != null && _station.GetOwningStation(camera) == ev.Station;
    }
}
