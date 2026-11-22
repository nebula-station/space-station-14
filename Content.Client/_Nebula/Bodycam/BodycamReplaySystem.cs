// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client.Replay.Spectator;
using Robust.Client.Player;
using Robust.Client.Replays.Playback;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace Content.Client._Nebula.Bodycam;

public sealed partial class BodycamReplaySystem : EntitySystem
{
    [Dependency] private IReplayPlaybackManager _playback = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private ReplaySpectatorSystem _spectator = default!;

    private NetEntity? _target;
    private bool _available;
    private float _accumulator;

    public override void Initialize()
    {
        base.Initialize();
        _playback.ReplayPlaybackStarted += OnStarted;
        _playback.ReplayPlaybackStopped += OnStopped;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playback.ReplayPlaybackStarted -= OnStarted;
        _playback.ReplayPlaybackStopped -= OnStopped;
    }

    private void OnStarted(MappingDataNode metadata, List<object> objects)
    {
        _target = null;
        _available = false;
        if (metadata.TryGet("bodycamTarget", out ValueDataNode? value) && int.TryParse(value.Value, out var id))
            _target = new NetEntity(id);
    }

    private void OnStopped()
    {
        _target = null;
    }

    private void PlaceOnMap()
    {
        var maps = EntityQueryEnumerator<MapComponent>();
        if (maps.MoveNext(out var map, out _))
            _spectator.SpawnSpectatorGhost(new EntityCoordinates(map, Vector2.Zero), false);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_target == null || _playback.Replay == null)
            return;

        _accumulator += frameTime;
        if (_accumulator < 0.25f)
            return;
        _accumulator = 0;

        var available = TryGetEntity(_target, out var uid)
                        && !TerminatingOrDeleted(uid)
                        && Transform(uid.Value).MapID != MapId.Nullspace;

        if (available && !_available && _player.LocalEntity != uid)
            _spectator.SpectateEntity(uid!.Value);
        else if (!available && (_player.LocalEntity is not { } local || TerminatingOrDeleted(local)))
            PlaceOnMap();

        _available = available;
    }
}
