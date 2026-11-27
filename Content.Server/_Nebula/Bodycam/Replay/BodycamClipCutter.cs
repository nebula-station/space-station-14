// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections;
using System.IO.Compression;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Content.Shared._Nebula.Bodycam;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Replays;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using static Robust.Shared.Replays.ReplayConstants;

namespace Content.Server._Nebula.Bodycam.Replay;

public sealed class BodycamClipException(string reason) : Exception(reason)
{
    public readonly string Reason = reason;
}

public sealed class BodycamClipRequest
{
    public required string SourcePath;
    public required GameTick FromTick;
    public required GameTick ToTick;
    public required int TickRate;
    public required HashSet<ushort> HiddenComponents;
    public required HashSet<ushort> GridComponents;
    public ushort? MapComponent;
    public ushort? GasOverlayComponent;
    public ushort? UserInterfaceComponent;
    public NetEntity? Camera;
    public string Label = string.Empty;
}

public sealed class BodycamClipResult
{
    public required ArraySegment<byte> Zip;
    public required GameTick FirstTick;
    public required GameTick LastTick;
}

public sealed class BodycamClipCutter(IRobustSerializer serializer)
{
    public const string MetaKeyTarget = "bodycamTarget";
    public const string MetaKeyLabel = "bodycamLabel";

    private static readonly ConcurrentDictionary<Type, FieldInfo[]> ReferenceFields = new();
    private static readonly ConcurrentDictionary<Type, FieldInfo?> ActorFields = new();

    private sealed class CameraState
    {
        public bool Enabled;
        public readonly HashSet<NetEntity> View = new();
        public NetCoordinates Position;
        public float Range;
        public bool Dirty = true;
        public bool Moved = true;
    }

    public BodycamClipResult Cut(BodycamClipRequest request)
    {
        using var reader = new ReplayStreamReader(serializer, request.SourcePath);
        var meta = reader.ReadYaml(FileMeta) ?? throw new BodycamClipException("bodycam-footage-error-corrupt");

        var (stringHash, _) = serializer.GetStringSerializerPackage();
        if (Value(meta, MetaKeyTypeHash) != Convert.ToHexString(serializer.GetSerializableTypesHash())
            || Value(meta, MetaKeyStringHash) != Convert.ToHexString(stringHash))
        {
            throw new BodycamClipException("bodycam-footage-error-version");
        }

        var filtered = request.Camera != null;
        var hidden = request.HiddenComponents;
        var camera = new CameraState();

        var fold = new EntityStateFold();
        var players = new Dictionary<NetUserId, SessionState>();
        var cvars = new Dictionary<string, object>();
        (TimeSpan, GameTick) timeBase = (
            new TimeSpan(long.Parse(Value(meta, MetaKeyBaseTime) ?? "0")),
            new GameTick(uint.Parse(Value(meta, MetaKeyBaseTick) ?? "0")));
        var earlyUploads = new List<object>();
        var tickUploads = new List<object>();
        var grids = new HashSet<NetEntity>();
        var maps = new HashSet<NetEntity>();
        var seenMaps = new HashSet<NetEntity>();
        var gasGrids = new HashSet<NetEntity>();
        var sentGas = new Dictionary<NetEntity, HashSet<Vector2i>>();

        using var writer = new ReplayClipWriter(serializer);
        GameTick? first = null;
        var last = GameTick.Zero;
        var clipVisible = new HashSet<NetEntity>();
        var clipKnown = new HashSet<NetEntity>();
        var clientFull = new Dictionary<NetEntity, HashSet<ushort>>();
        var lastSeen = GameTick.Zero;

        foreach (var (state, messages) in reader.ReadHistory())
        {
            var tick = state.ToSequence;
            if (tick > request.ToTick)
                break;

            lastSeen = tick;

            foreach (var player in state.PlayerStates.Span)
                players[player.UserId] = player;

            foreach (var entity in state.EntityStates.Span)
            {
                if (filtered)
                    StripActors(entity, request.UserInterfaceComponent);

                fold.Apply(entity);
                foreach (var change in entity.ComponentChanges.Span)
                {
                    if (request.GridComponents.Contains(change.NetID))
                        grids.Add(entity.NetEntity);
                    if (change.NetID == request.MapComponent)
                        maps.Add(entity.NetEntity);
                    if (change.NetID == request.GasOverlayComponent)
                        gasGrids.Add(entity.NetEntity);
                }
            }

            foreach (var deleted in state.EntityDeletions.Span)
            {
                fold.Delete(deleted);
                grids.Remove(deleted);
                maps.Remove(deleted);
                seenMaps.Remove(deleted);
                gasGrids.Remove(deleted);
                sentGas.Remove(deleted);
                if (camera.View.Remove(deleted))
                    camera.Dirty = true;
            }

            tickUploads.Clear();
            foreach (var message in messages.Messages)
            {
                switch (message)
                {
                    case BodycamReplayStatusEvent status when status.Camera == request.Camera:
                        camera.Enabled = status.Enabled;
                        if (!status.Enabled)
                            camera.View.Clear();
                        camera.Dirty = true;
                        camera.Moved = true;
                        break;
                    case BodycamReplayViewEvent view when view.Camera == request.Camera:
                        if (view.Reset)
                            camera.View.Clear();
                        camera.View.UnionWith(view.Entered);
                        camera.View.ExceptWith(view.Left);
                        foreach (var entity in view.Entered)
                        {
                            if (maps.Contains(entity))
                                seenMaps.Add(entity);
                        }

                        camera.Position = view.Position;
                        camera.Range = view.Range;
                        camera.Dirty = true;
                        camera.Moved = true;
                        break;
                    case ReplayMessage.CvarChangeMsg cvar when first == null:
                        foreach (var (name, value) in cvar.ReplicatedCvars)
                            cvars[name] = value;
                        timeBase = cvar.TimeBase;
                        break;
                    case EntityEventArgs:
                    case ChatMessage:
                    case ReplayMessage.CvarChangeMsg:
                        break;
                    default:
                        tickUploads.Add(message);
                        break;
                }
            }

            if (first == null && (tick < request.FromTick || filtered && (!camera.Enabled || camera.View.Count == 0)))
            {
                earlyUploads.AddRange(tickUploads);
                continue;
            }

            if (first == null)
            {
                first = tick;
                var visible = filtered ? CurrentVisible(camera, fold, seenMaps) : fold.Entities.Keys.ToHashSet();
                camera.Dirty = false;

                var states = new List<EntityState>(visible.Count);
                foreach (var entity in visible)
                {
                    if (fold.Snapshot(entity, filtered ? hidden : null, !filtered) is { } snapshot)
                    {
                        states.Add(snapshot);
                        if (filtered)
                            TrackFull(clientFull, snapshot);
                    }
                }

                if (filtered)
                    FilterGas(states, request.GasOverlayComponent, fold, camera, gasGrids, visible, sentGas);

                var firstMessages = new List<object>();
                if (cvars.Count > 0)
                {
                    firstMessages.Add(new ReplayMessage.CvarChangeMsg
                    {
                        ReplicatedCvars = cvars.Select(x => (x.Key, x.Value)).ToList(),
                        TimeBase = timeBase,
                    });
                }

                firstMessages.AddRange(Filter(messages, filtered, request.Camera, camera, visible, grids));

                writer.AddTick(
                    new GameState(GameTick.Zero, tick, 0, states.ToArray(),
                        filtered ? Array.Empty<SessionState>() : players.Values.ToArray(),
                        Array.Empty<NetEntity>()),
                    new ReplayMessage { Messages = firstMessages });

                clipVisible = visible;
                clipKnown = new HashSet<NetEntity>(visible);
                last = tick;
                continue;
            }

            if (!filtered)
            {
                writer.AddTick(
                    new GameState(last, tick, 0, state.EntityStates, state.PlayerStates, state.EntityDeletions),
                    new ReplayMessage { Messages = Filter(messages, false, null, camera, clipVisible, grids) });
                last = tick;
                continue;
            }

            var newVisible = clipVisible;
            if (camera.Dirty || state.EntityDeletions.Span.Length > 0)
            {
                newVisible = CurrentVisible(camera, fold, seenMaps);
                camera.Dirty = false;
            }

            var outStates = new List<EntityState>();
            foreach (var entity in state.EntityStates.Span)
            {
                if (newVisible.Contains(entity.NetEntity) && clipVisible.Contains(entity.NetEntity))
                    outStates.Add(StripForClient(entity, hidden, clientFull));
            }

            if (!ReferenceEquals(newVisible, clipVisible))
            {
                foreach (var entity in newVisible)
                {
                    if (clipVisible.Contains(entity))
                        continue;

                    if (fold.Snapshot(entity, hidden, false) is { } snapshot)
                    {
                        outStates.Add(snapshot);
                        TrackFull(clientFull, snapshot);
                        clipKnown.Add(entity);
                    }
                }
            }

            FilterGas(outStates, request.GasOverlayComponent, fold, camera, gasGrids, newVisible, sentGas);

            var deletions = new List<NetEntity>();
            foreach (var deleted in state.EntityDeletions.Span)
            {
                if (clipKnown.Remove(deleted))
                    deletions.Add(deleted);
                clientFull.Remove(deleted);
            }

            var outMessages = Filter(messages, true, request.Camera, camera, newVisible, grids);

            if (!ReferenceEquals(newVisible, clipVisible))
            {
                var left = new List<NetEntity>();
                foreach (var entity in clipVisible)
                {
                    if (!newVisible.Contains(entity) && clipKnown.Contains(entity))
                        left.Add(entity);
                }

                if (left.Count > 0)
                    outMessages.Add(new ReplayMessage.LeavePvs(left, tick));
            }

            writer.AddTick(
                new GameState(last, tick, 0, outStates.ToArray(), Array.Empty<SessionState>(), deletions.ToArray()),
                new ReplayMessage { Messages = outMessages });

            clipVisible = newVisible;
            last = tick;
        }

        if (first == null)
            throw new BodycamClipException(lastSeen < request.ToTick ? "bodycam-footage-error-pending" : "bodycam-footage-error-empty");

        writer.WriteRaw(FileStrings, reader.ReadRaw(FileStrings), CompressionLevel.Optimal);
        writer.WriteRaw(FileCvars, reader.ReadRaw(FileCvars), CompressionLevel.Optimal);

        var init = new List<object>();
        if (reader.ReadInit() is { } sourceInit)
        {
            foreach (var message in sourceInit.Messages)
            {
                if (message is not EntityEventArgs || !filtered)
                    init.Add(message);
            }
        }

        init.AddRange(earlyUploads);
        if (init.Count > 0)
            writer.WriteInit(new ReplayMessage { Messages = init });

        var period = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / request.TickRate);
        var startTime = timeBase.Item1 + (first.Value.Value - timeBase.Item2.Value) * period;
        var endTime = timeBase.Item1 + (last.Value - timeBase.Item2.Value) * period;

        var clipMeta = new MappingDataNode();
        foreach (var key in new[] { MetaKeyName, MetaKeyEngineVersion, MetaKeyForkId, MetaKeyForkVersion, MetaKeyTypeHash, MetaKeyStringHash, MetaKeyComponentHash, "roundId" })
        {
            if (Value(meta, key) is { } value)
                clipMeta[key] = new ValueDataNode(value);
        }

        clipMeta[MetaKeyTime] = new ValueDataNode(DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
        clipMeta[MetaKeyStartTick] = new ValueDataNode(first.Value.Value.ToString());
        clipMeta[MetaKeyBaseTick] = new ValueDataNode(timeBase.Item2.Value.ToString());
        clipMeta[MetaKeyBaseTime] = new ValueDataNode(timeBase.Item1.Ticks.ToString());
        clipMeta[MetaKeyStartTime] = new ValueDataNode(startTime.ToString());
        clipMeta[MetaKeyIsClientRecording] = new ValueDataNode(filtered.ToString());
        clipMeta[MetaKeyLabel] = new ValueDataNode(request.Label);
        if (request.Camera is { } target)
            clipMeta[MetaKeyTarget] = new ValueDataNode(target.Id.ToString());
        writer.WriteYaml(FileMeta, clipMeta);

        var zipFinal = new MappingDataNode();
        zipFinal[MetaFinalKeyEndTick] = new ValueDataNode(last.Value.ToString());
        zipFinal[MetaFinalKeyDuration] = new ValueDataNode((endTime - startTime).ToString());
        zipFinal[MetaFinalKeyEndTime] = new ValueDataNode(endTime.ToString());

        var bytes = FinishWithFinal(writer, zipFinal);
        return new BodycamClipResult { Zip = bytes, FirstTick = first.Value, LastTick = last };
    }

    private static ArraySegment<byte> FinishWithFinal(ReplayClipWriter writer, MappingDataNode final)
    {
        writer.Flush();
        final[MetaFinalKeyFileCount] = new ValueDataNode(writer.FileCount.ToString());
        final[MetaFinalKeyCompressedSize] = new ValueDataNode(writer.CompressedSize.ToString());
        final[MetaFinalKeyUncompressedSize] = new ValueDataNode(writer.UncompressedSize.ToString());
        writer.WriteYaml(FileMetaFinal, final);
        return writer.Finish();
    }

    private static void TrackFull(Dictionary<NetEntity, HashSet<ushort>> clientFull, EntityState state)
    {
        if (!clientFull.TryGetValue(state.NetEntity, out var set))
        {
            set = new HashSet<ushort>();
            clientFull[state.NetEntity] = set;
        }

        foreach (var change in state.ComponentChanges.Span)
        {
            if (change.State is not IComponentDeltaState)
                set.Add(change.NetID);
        }
    }

    private static EntityState StripForClient(EntityState state, HashSet<ushort> hidden, Dictionary<NetEntity, HashSet<ushort>> clientFull)
    {
        if (!clientFull.TryGetValue(state.NetEntity, out var full))
        {
            full = new HashSet<ushort>();
            clientFull[state.NetEntity] = full;
        }

        var span = state.ComponentChanges.Span;
        var changes = new List<ComponentChange>(span.Length);
        foreach (var change in span)
        {
            if (hidden.Contains(change.NetID))
                continue;

            if (change.State is IComponentDeltaState)
            {
                if (!full.Contains(change.NetID))
                    continue;
            }
            else
            {
                full.Add(change.NetID);
            }

            changes.Add(change);
        }

        HashSet<ushort>? netComps = null;
        if (state.NetComponents != null)
        {
            netComps = new HashSet<ushort>(state.NetComponents);
            netComps.ExceptWith(hidden);
        }

        return new EntityState(state.NetEntity, changes, state.EntityLastModified, netComps);
    }

    private static HashSet<NetEntity> CurrentVisible(CameraState camera, EntityStateFold fold, HashSet<NetEntity> maps)
    {
        var set = new HashSet<NetEntity>(maps);
        if (!camera.Enabled)
            return set;

        foreach (var entity in camera.View)
        {
            if (fold.Entities.ContainsKey(entity))
                set.Add(entity);
        }

        return set;
    }

    private static void StripActors(EntityState state, ushort? uiComponent)
    {
        if (uiComponent == null)
            return;

        foreach (var change in state.ComponentChanges.Span)
        {
            if (change.NetID != uiComponent || change.State == null)
                continue;

            var field = ActorFields.GetOrAdd(change.State.GetType(), t => t.GetField("Actors", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            if (field?.GetValue(change.State) is IDictionary actors)
                actors.Clear();
        }
    }

    private static void FilterGas(
        List<EntityState> states,
        ushort? gasComponent,
        EntityStateFold fold,
        CameraState camera,
        HashSet<NetEntity> gasGrids,
        HashSet<NetEntity> visible,
        Dictionary<NetEntity, HashSet<Vector2i>> sent)
    {
        if (gasComponent is not { } gasId)
            return;

        HashSet<NetEntity>? handled = null;
        for (var i = 0; i < states.Count; i++)
        {
            var state = states[i];
            var span = state.ComponentChanges.Span;
            var index = -1;
            for (var j = 0; j < span.Length; j++)
            {
                if (span[j].NetID == gasId)
                {
                    index = j;
                    break;
                }
            }

            if (index < 0)
                continue;

            var changes = span.ToArray().ToList();
            changes[index] = BuildGas(state.NetEntity, gasId, fold, camera, span[index].LastModifiedTick, sent);
            states[i] = new EntityState(state.NetEntity, changes, state.EntityLastModified, state.NetComponents);
            (handled ??= new()).Add(state.NetEntity);
        }

        if (!camera.Moved)
            return;

        camera.Moved = false;
        foreach (var grid in gasGrids)
        {
            if (handled?.Contains(grid) == true || !visible.Contains(grid))
                continue;

            var keys = GasKeys(grid, gasId, fold, camera);
            if (sent.TryGetValue(grid, out var previous) && previous.SetEquals(keys))
                continue;

            if (!fold.Entities.TryGetValue(grid, out var entry))
                continue;

            var change = BuildGas(grid, gasId, fold, camera, entry.LastModified, sent);
            states.Add(new EntityState(grid, new List<ComponentChange> { change }, entry.LastModified, null));
        }
    }

    private static GasTileOverlayState? FullGas(NetEntity grid, ushort gasId, EntityStateFold fold)
    {
        return fold.Entities.TryGetValue(grid, out var entry)
               && entry.Components.TryGetValue(gasId, out var change)
            ? change.State as GasTileOverlayState
            : null;
    }

    private static bool InRange(Vector2i index, CameraState camera)
    {
        if (!camera.Enabled)
            return false;

        var size = SharedGasTileOverlaySystem.ChunkSize;
        var minX = index.X * size;
        var minY = index.Y * size;
        var p = camera.Position.Position;
        var dx = MathF.Max(MathF.Max(minX - p.X, 0), p.X - (minX + size));
        var dy = MathF.Max(MathF.Max(minY - p.Y, 0), p.Y - (minY + size));
        var range = camera.Range + 1;
        return dx * dx + dy * dy <= range * range;
    }

    private static HashSet<Vector2i> GasKeys(NetEntity grid, ushort gasId, EntityStateFold fold, CameraState camera)
    {
        var keys = new HashSet<Vector2i>();
        if (camera.Position.NetEntity != grid || FullGas(grid, gasId, fold) is not { } full)
            return keys;

        foreach (var index in full.Chunks.Keys)
        {
            if (InRange(index, camera))
                keys.Add(index);
        }

        return keys;
    }

    private static ComponentChange BuildGas(
        NetEntity grid,
        ushort gasId,
        EntityStateFold fold,
        CameraState camera,
        GameTick modified,
        Dictionary<NetEntity, HashSet<Vector2i>> sent)
    {
        var chunks = new Dictionary<Vector2i, GasOverlayChunk>();
        var keys = new HashSet<Vector2i>();
        if (camera.Position.NetEntity == grid && FullGas(grid, gasId, fold) is { } full)
        {
            foreach (var (index, chunk) in full.Chunks)
            {
                if (!InRange(index, camera))
                    continue;

                chunks[index] = chunk;
                keys.Add(index);
            }
        }

        sent[grid] = keys;
        return new ComponentChange(gasId, new GasTileOverlayState(chunks), modified);
    }

    private static List<object> Filter(
        ReplayMessage messages,
        bool filtered,
        NetEntity? target,
        CameraState camera,
        HashSet<NetEntity> visible,
        HashSet<NetEntity> grids)
    {
        var result = new List<object>();
        foreach (var message in messages.Messages)
        {
            switch (message)
            {
                case BodycamReplayChatEvent chat:
                    if (filtered && chat.Camera == target && camera.Enabled)
                        result.Add(chat.Message);
                    break;
                case BodycamReplayStatusEvent:
                case BodycamReplayViewEvent:
                    break;
                case ChatMessage chat:
                    if (!filtered)
                        result.Add(chat);
                    break;
                case PopupCursorEvent:
                    if (!filtered)
                        result.Add(message);
                    break;
                case EntityEventArgs ev:
                    if (!filtered || camera.Enabled && IsNearby(ev, camera, visible, grids, 0))
                        result.Add(ev);
                    break;
                default:
                    result.Add(message);
                    break;
            }
        }

        return result;
    }

    private static bool IsNearby(object obj, CameraState camera, HashSet<NetEntity> visible, HashSet<NetEntity> grids, int depth)
    {
        switch (obj)
        {
            case NetEntity entity:
                return entity.Valid && visible.Contains(entity) && !grids.Contains(entity);
            case NetCoordinates coords:
                if (!coords.NetEntity.Valid)
                    return false;
                if (coords.NetEntity == camera.Position.NetEntity)
                    return (coords.Position - camera.Position.Position).Length() <= camera.Range;
                return visible.Contains(coords.NetEntity) && !grids.Contains(coords.NetEntity);
        }

        if (depth > 2)
            return false;

        if (obj is IEnumerable enumerable and not string)
        {
            foreach (var item in enumerable)
            {
                if (item != null && IsNearby(item, camera, visible, grids, depth + 1))
                    return true;
            }

            return false;
        }

        foreach (var field in GetReferenceFields(obj.GetType()))
        {
            if (field.GetValue(obj) is { } value && IsNearby(value, camera, visible, grids, depth + 1))
                return true;
        }

        return false;
    }

    private static FieldInfo[] GetReferenceFields(Type type)
    {
        return ReferenceFields.GetOrAdd(type, t => t
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => MayReference(f.FieldType))
            .ToArray());
    }

    private static bool MayReference(Type type)
    {
        if (type == typeof(NetEntity) || type == typeof(NetCoordinates)
            || type == typeof(NetEntity?) || type == typeof(NetCoordinates?))
        {
            return true;
        }

        if (type == typeof(string) || type.IsPrimitive || type.IsEnum)
            return false;

        if (typeof(IEnumerable).IsAssignableFrom(type))
            return true;

        return type.IsValueType && type.IsGenericType && type.FullName?.StartsWith("System.ValueTuple") == true;
    }

    private static string? Value(MappingDataNode node, string key)
    {
        return node.TryGet(key, out ValueDataNode? value) ? value.Value : null;
    }
}
