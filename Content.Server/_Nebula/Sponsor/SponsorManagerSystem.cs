// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Content.Server._Nebula.Administration;
using Content.Shared._Nebula.Sponsor;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Nebula.Sponsor;

public sealed partial class SponsorManagerSystem : EntitySystem
{
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private NebulaServerApi _nebulaServerApi = default!;

    private readonly Dictionary<NetUserId, SponsorStatus> _cachedPlayers = [];

    public override void Initialize()
    {
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        base.Initialize();
    }

    [SubscribeNetworkEvent]
    private void OnStatusRequest(SponsorStatusRequest msg, EntitySessionEventArgs args)
    {
        SendStatus(args.SenderSession);
    }

    private void SendStatus(ICommonSession session)
    {
        var rank = _cachedPlayers.TryGetValue(session.UserId, out var status) ? status.Rank : (int?) null;
        RaiseNetworkEvent(new SponsorStatusResponse { Rank = rank }, session);
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        switch (args.NewStatus)
        {
            case SessionStatus.Connected:
                await RefreshCachedPlayerStatus(args.Session);
                break;
            case SessionStatus.Disconnected:
                _cachedPlayers.Remove(args.Session.UserId);
                break;
        }
    }

    private async Task RefreshCachedPlayerStatus(ICommonSession session)
    {
        var status = await _nebulaServerApi.GetSponsorUserDataOrNull(session.UserId);
        if (status != null)
            _cachedPlayers[session.UserId] = status;
        else
            _cachedPlayers.Remove(session.UserId);

        if (session.Status is SessionStatus.Connected or SessionStatus.InGame)
            SendStatus(session);
    }

    public bool IsSponsor(NetUserId userId)
    {
        return _cachedPlayers.ContainsKey(userId);
    }

    public bool GetCachedUserInfo(NetUserId userId, [NotNullWhen(true)] out SponsorStatus? status)
    {
        return _cachedPlayers.TryGetValue(userId, out status);
    }
}
