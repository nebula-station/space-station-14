// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Nebula.Administration;
using Content.Shared._Nebula.Discord.Linking;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server._Nebula.Discord.Linking;

public sealed partial class DiscordLinkingManager : IPostInjectInit
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private NebulaServerApi _serverApi = default!;

    private readonly Dictionary<NetUserId, (Guid Code, TimeSpan Expires)> _activeCodes = new();
    private readonly HashSet<NetUserId> _pending = new();
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(3);

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<DiscordLinkingRequestMsg>(OnRequest);
        _net.RegisterNetMessage<DiscordLinkingCodeMsg>();
    }

    private async void OnRequest(DiscordLinkingRequestMsg message)
    {
        var channel = message.MsgChannel;
        var user = channel.UserId;
        var now = _timing.RealTime;

        if (_activeCodes.TryGetValue(user, out var active) && active.Expires > now)
        {
            _net.ServerSendMessage(new DiscordLinkingCodeMsg { Status = DiscordLinkingCodeStatus.Ok, Code = active.Code }, channel);
            return;
        }

        if (!_pending.Add(user))
            return;

        try
        {
            var code = Guid.NewGuid();

            var status = await _serverApi.RegisterLinkingCode(user.UserId, code);

            if (status == DiscordLinkingCodeStatus.Ok)
                _activeCodes[user] = (code, now + CodeLifetime);
            else
                _activeCodes.Remove(user);

            if (channel.IsConnected)
            {
                _net.ServerSendMessage(new DiscordLinkingCodeMsg
                {
                    Status = status,
                    Code = status == DiscordLinkingCodeStatus.Ok ? code : default,
                }, channel);
            }
        }
        finally
        {
            _pending.Remove(user);
        }
    }
}
