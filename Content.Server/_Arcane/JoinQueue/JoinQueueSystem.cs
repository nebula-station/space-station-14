// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/dfbda555db9acabf3827cf3b0785c6deaa5c1a8b/Content.Arcane.Server/JoinQueue/JoinQueueSystem.cs

using System.Linq;
using Content.Goobstation.Common.CCVar;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Connection;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Maps;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared._Arcane.JoinQueue;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Arcane.JoinQueue;

/// <summary>
/// Defers only entry to the game. The existing connection manager still owns all admission restrictions.
/// </summary>
public sealed partial class JoinQueueSystem : EntitySystem
{
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IConnectionManager _connection = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IServerNetManager _net = default!;
    [Dependency] private UserDbDataManager _userDb = default!;
    [Dependency] private ITaskManager _tasks = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IAdminManager _admins = default!;
    [Dependency] private IGameMapManager _maps = default!;
    [Dependency] private ServerGameTicker _ticker = default!;

    private static readonly TimeSpan ReconnectGrace = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan InfoInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ScoreInterval = TimeSpan.FromSeconds(1);
    private const int DisplayPlayerLimit = 100;

    private readonly JoinQueueState<ICommonSession> _queue = new();
    private readonly Dictionary<NetUserId, PendingAdmission> _pending = [];
    private readonly Dictionary<NetUserId, Dictionary<QueueMiniGameKind, ScoreState>> _scores = [];
    private readonly Dictionary<NetUserId, string> _scoreNames = [];
    private readonly Dictionary<NetUserId, WaitRecord> _waits = [];
    private readonly Queue<double> _recentWaits = [];
    private TimeSpan _nextInfo;
    private TimeSpan _nextScoreBroadcast;
    private TimeSpan _nextPrune;
    private bool _scoresDirty;
    private bool _scoresPending;
    private bool _queueDirty;
    private bool _enabled;
    private bool _shutdown;

    internal int PlayerInQueueCount => _queue.Entries.Count(e => e.Session != null && !e.Reserved);
    internal int ActualPlayersCount => _players.Sessions.Count(IsAdmitted);
    internal double AverageQueueWait => _recentWaits.Count > 0 ? _recentWaits.Average() : -1;

    public override void Initialize()
    {
        base.Initialize();
        _net.RegisterNetMessage<QueueUpdateMessage>();
        _net.RegisterNetMessage<QueueMiniGameScoreMessage>(OnScore);
        _userDb.AddOnFinishLoad(OnDataLoaded);
        _players.PlayerStatusChanged += OnStatusChanged;
        _admins.OnPermsChanged += OnPermissionsChanged;
        Subs.CVar(_cfg, GoobCVars.QueueEnabled, OnEnabled, true);
        Subs.CVar(_cfg, CCVars.SoftMaxPlayers, _ => ProcessQueue());
        Subs.CVar(_cfg, CCVars.AdminsCountForMaxPlayers, _ => ProcessQueue());
    }

    public override void Shutdown()
    {
        // UserDb's callbacks belong to its server lifetime and have no removal API.
        // Pending completions must therefore explicitly stop using this system on shutdown.
        _shutdown = true;
        _players.PlayerStatusChanged -= OnStatusChanged;
        _admins.OnPermsChanged -= OnPermissionsChanged;
        _queue.Clear();
        _pending.Clear();
        base.Shutdown();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (!_enabled)
            return;
        var now = _timing.RealTime;
        if (now >= _nextPrune)
        {
            if (_queue.Prune(now))
                _queueDirty = true;
            _nextPrune = now + ScoreInterval;
            FlushPendingScores(now);
        }
        if (now < _nextScoreBroadcast || now < _nextInfo && !_scoresDirty && !_queueDirty)
            return;
        SendUpdates();
    }

    [SubscribeLocalEvent]
    private void OnJoinRequested(ref PlayerJoinRequestedEvent args)
    {
        if (!_enabled || args.Deferred)
            return;
        _queue.Connect(args.Session.UserId, args.Session, _timing.RealTime);
        args.Deferred = true;
        QueueChanged();
    }

    [SubscribeLocalEvent]
    private void OnCountRequested(ref GetAdmittedPlayerCountEvent args)
    {
        if (_enabled)
            args.Count = ActualPlayersCount;
    }

    private void OnDataLoaded(ICommonSession session)
    {
        _tasks.RunOnMainThread(() =>
        {
            if (!_shutdown && _enabled && _queue.IsCurrent(session.UserId, session) && IsCurrentConnected(session))
                LoadPrivileges(session);
        });
    }

    private async void LoadPrivileges(ICommonSession session)
    {
        try
        {
            var privileged = await _connection.HasPrivilegedJoin(session.UserId);
            _tasks.RunOnMainThread(() => FinishDataLoaded(session, privileged));
        }
        catch (Exception exception)
        {
            Log.Error($"Failed to determine queue admission for {session.UserId}: {exception}");
            _tasks.RunOnMainThread(() => FinishDataLoaded(session, false));
        }
    }

    private void FinishDataLoaded(ICommonSession session, bool privileged)
    {
        if (_shutdown || !_queue.IsCurrent(session.UserId, session) || !IsCurrentConnected(session))
            return;
        if (!_queue.MarkReady(session.UserId, session))
            return;
        if (privileged && _queue.Reserve(session.UserId, session, out var entry))
            Admit(entry, countTowardsEstimate: false);
        ProcessQueue();
    }

    private void OnStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        var session = args.Session;
        if (args.NewStatus == SessionStatus.Disconnected)
        {
            _queue.Disconnect(session.UserId, session, _timing.RealTime + ReconnectGrace);
            if (_pending.TryGetValue(session.UserId, out var pending) && ReferenceEquals(pending.Session, session))
                _pending.Remove(session.UserId);
            ClearScores(session.UserId);
            ProcessQueue();
        }
        else if (args.NewStatus == SessionStatus.InGame)
        {
            var hasPending = _pending.TryGetValue(session.UserId, out var pending) && ReferenceEquals(pending.Session, session);
            if (_queue.Remove(session.UserId, session, out var queued))
            {
                RecordWait(queued);
                if (hasPending && pending.CountTowardsEstimate)
                {
                    _recentWaits.Enqueue(WaitSeconds(queued));
                    while (_recentWaits.Count > 20)
                        _recentWaits.Dequeue();
                }
                ClearScores(session.UserId);
            }
            if (hasPending)
                _pending.Remove(session.UserId);
            ProcessQueue();
        }
    }

    private void OnPermissionsChanged(AdminPermsChangedEventArgs args) => ProcessQueue();

    private void OnEnabled(bool enabled)
    {
        _enabled = enabled;
        if (enabled)
        {
            ProcessQueue();
            return;
        }
        var sessions = _queue.Entries.Select(e => e.Session).OfType<ICommonSession>().ToArray();
        _queue.Clear();
        _pending.Clear();
        QueueChanged();
        foreach (var session in sessions)
        {
            ClearScores(session.UserId);
            if (IsCurrentConnected(session))
                session.Channel.Disconnect(Loc.GetString("queue-disabled-disconnect-reason"));
        }
    }

    private bool IsCurrentConnected(ICommonSession session) => session.Status == SessionStatus.Connected
        && _players.TryGetSessionById(session.UserId, out var current) && ReferenceEquals(current, session);

    private bool IsAdmitted(ICommonSession session) => session.Status == SessionStatus.InGame
        || session.Status == SessionStatus.Connected
        && _pending.TryGetValue(session.UserId, out var pending) && ReferenceEquals(pending.Session, session);

    private int CountedPlayers() => _players.Sessions.Count(session => IsAdmitted(session)
        && (_cfg.GetCVar(CCVars.AdminsCountForMaxPlayers) || !_admins.IsAdmin(session)));

    private void ProcessQueue()
    {
        if (_shutdown || !_enabled)
            return;
        _queue.Prune(_timing.RealTime);
        var count = CountedPlayers();
        var maximum = Math.Max(0, _cfg.GetCVar(CCVars.SoftMaxPlayers));
        while (count < maximum && _queue.TryReserve(out var entry))
        {
            if (entry.Session is not { } session)
                continue;
            if (!IsCurrentConnected(session))
            {
                _queue.Disconnect(entry.User, session, _timing.RealTime + ReconnectGrace);
                continue;
            }
            if (!Admit(entry, countTowardsEstimate: true))
                continue;
            if (_cfg.GetCVar(CCVars.AdminsCountForMaxPlayers) || !_admins.IsAdmin(session))
                count++;
        }
        QueueChanged();
    }

    private bool Admit(JoinQueueState<ICommonSession>.Entry entry, bool countTowardsEstimate)
    {
        if (entry.Session is not { } session || !IsCurrentConnected(session)
            || _pending.TryGetValue(session.UserId, out var existing) && ReferenceEquals(existing.Session, session))
            return false;

        _pending[session.UserId] = new PendingAdmission(session, countTowardsEstimate);
        Timer.Spawn(0, () =>
        {
            if (_shutdown || !_pending.TryGetValue(session.UserId, out var current) || !ReferenceEquals(current.Session, session))
                return;
            if (!IsCurrentConnected(session))
            {
                _pending.Remove(session.UserId);
                _queue.Disconnect(session.UserId, session, _timing.RealTime + ReconnectGrace);
                ProcessQueue();
                return;
            }
            // Keep the reservation until the synchronous InGame status event takes over the count.
            _players.JoinGame(session);
        });
        return true;
    }

    private double WaitSeconds(JoinQueueState<ICommonSession>.Entry entry) =>
        Math.Max(0, (_timing.RealTime - entry.WaitedSince).TotalSeconds);

    private void RecordWait(JoinQueueState<ICommonSession>.Entry entry)
    {
        if (entry.Session is not { } session)
            return;
        var waited = (float) WaitSeconds(entry);
        if (_waits.TryGetValue(entry.User, out var previous))
            waited = Math.Max(waited, previous.Seconds);
        _waits[entry.User] = new WaitRecord(session.Name, waited);
    }

    private readonly record struct PendingAdmission(ICommonSession Session, bool CountTowardsEstimate);

    private void QueueChanged()
    {
        // Admission snapshots are used off-thread by connection/status checks; do not throttle them with UI updates.
        _ticker.GetAdmittedPlayerCount();
        _queueDirty = true;
    }

    private void SendUpdates()
    {
        if (_shutdown || !_enabled)
            return;
        var entries = _queue.Entries.Where(e => !e.Reserved && e.Session is { } session && IsCurrentConnected(session)).ToArray();
        foreach (var entry in entries)
            RecordWait(entry);
        var longest = _waits.OrderByDescending(p => p.Value.Seconds).Take(100).ToArray();
        if (_waits.Count > 200 + entries.Length)
        {
            var retain = longest.Select(p => p.Key).Concat(entries.Select(e => e.User)).ToHashSet();
            foreach (var user in _waits.Keys.ToArray())
                if (!retain.Contains(user))
                    _waits.Remove(user);
        }

        var names = entries.Select(e => e.Session!.Name).Take(DisplayPlayerLimit).ToList();
        var waits = entries.Take(DisplayPlayerLimit).Select(e => (float) WaitSeconds(e)).ToList();
        var board = BuildScores();
        var average = AverageQueueWait;
        var preset = _ticker.Decoy ?? _ticker.CurrentPreset ?? _ticker.Preset;
        var mode = preset == null ? string.Empty : preset.ModeTitle.Id;
        var elapsed = _ticker.RunLevel >= GameRunLevel.InRound
            ? (int) (_timing.CurTime - _ticker.RoundStartTimeSpan).TotalMinutes : 0;
        var mapName = _maps.GetSelectedMap()?.MapName ?? string.Empty;
        var playerCount = ActualPlayersCount;
        var maximumPlayers = _cfg.GetCVar(CCVars.SoftMaxPlayers);
        var leaderboardNames = longest.Select(p => p.Value.Name).ToList();
        var leaderboardSeconds = longest.Select(p => p.Value.Seconds).ToList();
        for (var i = 0; i < entries.Length; i++)
        {
            var session = entries[i].Session!;
            session.Channel.SendMessage(new QueueUpdateMessage
            {
                Total = entries.Length,
                Position = i + 1,
                EstimatedWaitSeconds = average < 0 ? -1 : (float) (average * (i + 1) / entries.Length),
                MapName = mapName,
                GameMode = mode,
                ServerPlayerCount = playerCount,
                MaxPlayerCount = maximumPlayers,
                RoundDurationMinutes = elapsed,
                YourName = session.Name,
                PlayerNames = names,
                PlayerWaitSeconds = waits,
                QueueWaitLeaderboardNames = leaderboardNames,
                QueueWaitLeaderboardSeconds = leaderboardSeconds,
                YourQueueWaitLeaderboardIndex = Array.FindIndex(longest, p => p.Key == session.UserId),
                MiniGameLeaderboard = board,
            });
        }
        _scoresDirty = false;
        _queueDirty = false;
        _nextInfo = _timing.RealTime + InfoInterval;
        _nextScoreBroadcast = _timing.RealTime + ScoreInterval;
    }

    private void OnScore(QueueMiniGameScoreMessage message)
    {
        var maximum = GetMaxScore(message.Game);
        if (!_enabled || !_queue.TryGet(message.MsgChannel.UserId, out var entry)
            || entry.Session is not { } session || !ReferenceEquals(session.Channel, message.MsgChannel)
            || !IsCurrentConnected(session) || maximum == 0)
            return;
        if (!_scores.TryGetValue(entry.User, out var scores))
            _scores[entry.User] = scores = new();
        var now = _timing.RealTime;
        var score = Math.Clamp(message.Score, 0, maximum);
        if (!scores.TryGetValue(message.Game, out var state))
        {
            scores[message.Game] = new ScoreState { Score = score, PendingScore = score, UpdatedAt = now };
            _scoreNames[entry.User] = session.Name;
            _scoresDirty |= score > 0;
            return;
        }
        if (score <= state.PendingScore)
            return;

        state.PendingScore = score;
        if (now - state.UpdatedAt < ScoreInterval)
        {
            _scoresPending = true;
            return;
        }
        state.Score = score;
        state.UpdatedAt = now;
        _scoresDirty = true;
    }

    private void FlushPendingScores(TimeSpan now)
    {
        if (!_scoresPending)
            return;

        _scoresPending = false;
        foreach (var scores in _scores.Values)
        foreach (var state in scores.Values)
        {
            if (state.PendingScore <= state.Score)
                continue;
            if (now - state.UpdatedAt < ScoreInterval)
            {
                _scoresPending = true;
                continue;
            }
            state.Score = state.PendingScore;
            state.UpdatedAt = now;
            _scoresDirty = true;
        }
    }

    internal List<QueueMiniGameLeaderboardEntry> BuildScores()
    {
        var board = new List<QueueMiniGameLeaderboardEntry>(15);
        foreach (var game in Enum.GetValues<QueueMiniGameKind>())
        {
            var best = _scores.Where(p => p.Value.TryGetValue(game, out var state) && state.Score > 0)
                .OrderByDescending(p => p.Value[game].Score).Take(5);
            board.AddRange(best.Select(p => new QueueMiniGameLeaderboardEntry(game, _scoreNames[p.Key], p.Value[game].Score)));
        }
        return board;
    }

    private void ClearScores(NetUserId user)
    {
        _scoresDirty |= _scores.Remove(user);
        _scoreNames.Remove(user);
    }

    internal static int GetMaxScore(QueueMiniGameKind game) => game switch
    {
        // These games continue indefinitely; only the message's integer range bounds the score.
        QueueMiniGameKind.Gyruss => int.MaxValue,
        QueueMiniGameKind.GoGoShitcurity => int.MaxValue,
        // Seven fixed waves, including every crawler and every hit on their cores.
        QueueMiniGameKind.SpaceInvaders => 6480,
        _ => 0,
    };

    private sealed class ScoreState
    {
        public int Score;
        public int PendingScore;
        public TimeSpan UpdatedAt;
    }
    private readonly record struct WaitRecord(string Name, float Seconds);
}
