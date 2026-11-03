// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Content.Server._Nebula.Administration;
using Content.Shared._Nebula.Sponsor;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Nebula.Sponsor;

public sealed partial class SponsorManagerSystem : EntitySystem
{
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private NebulaServerApi _nebulaServerApi = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private readonly Dictionary<NetUserId, SponsorStatus> _cachedPlayers = [];

    public override void Initialize()
    {
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        base.Initialize();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
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

        // The player may have left while the bot was answering, Disconnected has already cleared the cache.
        if (session.Status is not (SessionStatus.Connected or SessionStatus.InGame))
            return;

        if (status != null)
            _cachedPlayers[session.UserId] = status;
        else
            _cachedPlayers.Remove(session.UserId);

        SendStatus(session);
    }

    /// <summary>
    /// Pretends the player has this subscription rank until they leave. Weekly tokens are counted by the bot and stay
    /// as they are.
    /// </summary>
    /// <returns>False if no <see cref="SponsorTierPrototype"/> has this rank.</returns>
    public bool TrySetFakeRank(ICommonSession session, int rank)
    {
        if (!_proto.EnumeratePrototypes<SponsorTierPrototype>().Any(tier => tier.Rank == rank))
            return false;

        _cachedPlayers[session.UserId] = new SponsorStatus { Rank = rank, StartedAt = DateTimeOffset.UtcNow };
        SendStatus(session);
        return true;
    }

    /// <summary>
    /// Replaces a rank from <see cref="TrySetFakeRank"/> with the real status from the bot.
    /// </summary>
    public Task ClearFakeRank(ICommonSession session)
    {
        return RefreshCachedPlayerStatus(session);
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
