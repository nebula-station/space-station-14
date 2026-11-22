// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client.Replay.Spectator;
using Robust.Client.Player;
using Robust.Client.Replays.Playback;
using Robust.Client.Replays.UI;
using Robust.Client.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Replays;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace Content.Client._Nebula.Bodycam;

public sealed partial class BodycamReplaySystem : EntitySystem
{
    [Dependency] private IReplayPlaybackManager _playback = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private ReplaySpectatorSystem _spectator = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    private NetEntity? _target;
    private EntityUid? _viewing;
    private bool _started;
    private float _accumulator;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesBefore.Add(typeof(ReplaySpectatorSystem));
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
        _viewing = null;
        _started = false;

        if (_playback.Replay is { } replay)
            CompleteCheckpointComponents(replay);

        if (metadata.TryGet("bodycamTarget", out ValueDataNode? value) && int.TryParse(value.Value, out var id))
            _target = new NetEntity(id);
    }

    private static void CompleteCheckpointComponents(ReplayData replay)
    {
        foreach (var checkpoint in replay.Checkpoints)
        {
            Complete(checkpoint.FullState.EntityStates.Value);
            if (checkpoint.AttachedStates is { } attached)
                Complete(attached.EntityStates.Value);
            if (checkpoint.DetachedStates is { } detached)
                Complete(detached);
        }
    }

    private static void Complete(IEnumerable<EntityState>? states)
    {
        if (states == null)
            return;

        foreach (var state in states)
        {
            if (state.NetComponents is not { } netComps || state.ComponentChanges.Value is not { } changes)
                continue;

            foreach (var change in changes)
                netComps.Add(change.NetID);
        }
    }

    private void OnStopped()
    {
        _target = null;
        _viewing = null;
    }

    private EntityUid? ViewTarget()
    {
        if (!TryGetEntity(_target, out var camera)
            || TerminatingOrDeleted(camera)
            || Transform(camera.Value).MapID == MapId.Nullspace)
        {
            return null;
        }

        return _container.TryGetOuterContainer(camera.Value, Transform(camera.Value), out var outer)
            ? outer.Owner
            : camera;
    }

    private void PlaceOnMap()
    {
        var maps = EntityQueryEnumerator<MapComponent>();
        if (maps.MoveNext(out var map, out _))
            _spectator.SpawnSpectatorGhost(new EntityCoordinates(map, Vector2.Zero), false);
    }

    private void HideSkipButtons()
    {
        if (_ui.GetActiveUIWidgetOrNull<ReplayControlWidget>() is not { } widget)
            return;

        widget.SkipBackButton.Visible = false;
        widget.SkipBackwardManyButton.Visible = false;
        widget.SkipForwardButton.Visible = false;
        widget.SkipForwardManyButton.Visible = false;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_target == null || _playback.Replay == null)
            return;

        if (_viewing != null && _player.LocalEntity == _viewing)
            _spectator.Direction = DirectionFlag.None;

        HideSkipButtons();

        _accumulator += frameTime;
        if (_accumulator < 0.25f)
            return;
        _accumulator = 0;

        var view = ViewTarget();
        if (view != null && _player.LocalEntity != view)
            _spectator.SpectateEntity(view.Value);
        else if (view == null && (_player.LocalEntity is not { } local || TerminatingOrDeleted(local)))
            PlaceOnMap();

        _viewing = view;

        if (!_started && view != null)
        {
            _started = true;
            _playback.Playing = true;
        }
    }
}
