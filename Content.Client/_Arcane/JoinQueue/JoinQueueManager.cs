// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/dfbda555db9acabf3827cf3b0785c6deaa5c1a8b/Content.Arcane.Client/JoinQueue/JoinQueueManager.cs

using Content.Shared._Arcane.JoinQueue;
using Robust.Client;
using Robust.Client.Audio;
using Robust.Client.State;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Client._Arcane.JoinQueue;

/// <summary>
/// Connection messages belong to the client application, rather than world systems recreated on reconnect.
/// </summary>
public sealed partial class JoinQueueManager
{
    [Dependency] private IClientNetManager _net = default!;
    [Dependency] private IBaseClient _client = default!;
    [Dependency] private IStateManager _states = default!;
    [Dependency] private IEntityManager _entities = default!;

    private QueueUpdateMessage? _pending;
    private bool _worldReady;
    private bool _wasQueued;

    public void Initialize()
    {
        _net.RegisterNetMessage<QueueUpdateMessage>(OnUpdate);
        _net.RegisterNetMessage<QueueMiniGameScoreMessage>();
        _client.PlayerJoinedServer += OnJoinedServer;
        _client.PlayerJoinedGame += OnJoinedGame;
        _client.RunLevelChanged += OnRunLevelChanged;
    }

    public void Shutdown()
    {
        _client.PlayerJoinedServer -= OnJoinedServer;
        _client.PlayerJoinedGame -= OnJoinedGame;
        _client.RunLevelChanged -= OnRunLevelChanged;
        _pending = null;
    }

    private void OnUpdate(QueueUpdateMessage message)
    {
        if (_client.RunLevel is ClientRunLevel.InGame or ClientRunLevel.SinglePlayerGame or ClientRunLevel.Initialize)
            return;
        _pending = message;
        ShowPending();
    }

    private void OnJoinedServer(object? sender, PlayerEventArgs args)
    {
        // This event follows GameStartedSetup; the preceding Connected run-level event does not.
        _worldReady = true;
        ShowPending();
    }

    private void ShowPending()
    {
        if (!_worldReady || _client.RunLevel != ClientRunLevel.Connected || _pending is not { } message
            || !ReferenceEquals(message.MsgChannel, _net.ServerChannel))
            return;
        if (_states.CurrentState is not QueueState)
            _states.RequestStateChange<QueueState>();
        if (_states.CurrentState is QueueState state)
        {
            _wasQueued = true;
            state.OnQueueUpdate(message);
        }
    }

    private void OnRunLevelChanged(object? sender, RunLevelChangedEventArgs args)
    {
        if (args.NewLevel is ClientRunLevel.Initialize or ClientRunLevel.Connecting)
        {
            _pending = null;
            _worldReady = false;
            _wasQueued = false;
        }
        else if (args.NewLevel == ClientRunLevel.InGame)
            _pending = null;
    }

    private void OnJoinedGame(object? sender, PlayerEventArgs args)
    {
        if (!_wasQueued)
            return;
        _wasQueued = false;
        if (_entities.TrySystem<AudioSystem>(out var audio))
            audio.PlayGlobal("/Audio/Effects/newplayerping.ogg", Filter.Local(), false);
    }
}
