// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading.Tasks;
using Content.Server._Nebula.Administration;
using Content.Shared._Nebula.Discord.Linking;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Nebula.Discord.Linking;

public sealed partial class DiscordLinkingSystem : EntitySystem
{
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private NebulaServerApi _nebulaServerApi = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly Dictionary<NetUserId, LinkStatus> _cachedPlayers = new();
    private readonly Dictionary<NetUserId, TimeSpan> _lastCheck = new();
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromSeconds(10);

    public override void Initialize()
    {
        IoCManager.Resolve<DiscordLinkingManager>();

        SubscribeNetworkEvent<VerifyStatusRequest>(OnVerifyStatusRequest);
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        base.Initialize();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private async void OnVerifyStatusRequest(VerifyStatusRequest msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;

        if (!_cachedPlayers.TryGetValue(session.UserId, out var status) || !status.Linked)
        {
            var now = _timing.RealTime;
            if (!_lastCheck.TryGetValue(session.UserId, out var last) || last + RecheckDelay <= now)
            {
                _lastCheck[session.UserId] = now;
                await RefreshPlayer(session);
                _cachedPlayers.TryGetValue(session.UserId, out status);
            }
        }

        if (IsConnected(session))
            RaiseNetworkEvent(new VerifyStatusResponse { Verified = status?.Linked ?? false }, session);
    }

    private async Task RefreshPlayer(ICommonSession session)
    {
        var status = await _nebulaServerApi.GetLinkedUserDataOrNull(session.UserId);

        // The player may have left while the bot was answering, Disconnected has already cleared the cache.
        if (status != null && IsConnected(session))
            _cachedPlayers[session.UserId] = status;
    }

    private static bool IsConnected(ICommonSession session)
    {
        return session.Status is SessionStatus.Connected or SessionStatus.InGame;
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        switch (args.NewStatus)
        {
            case SessionStatus.Connected:
                await RefreshPlayer(args.Session);
                break;
            case SessionStatus.Disconnected:
                _cachedPlayers.Remove(args.Session.UserId);
                _lastCheck.Remove(args.Session.UserId);
                break;
        }
    }

    public void ForceUpdateUserData(NetUserId userId)
    {
        _cachedPlayers.Remove(userId);
        if (_playerManager.TryGetSessionById(userId, out var session))
            _ = RefreshPlayer(session);
    }

    private LinkStatus GetCachedUserInfo(NetUserId userId)
    {
        return _cachedPlayers.GetValueOrDefault(userId) ?? new LinkStatus();
    }

    public bool IsUserLinked(NetUserId userId)
    {
        return GetCachedUserInfo(userId).Linked;
    }

    public bool IsUserVerified(NetUserId userId) => IsUserLinked(userId);
}
