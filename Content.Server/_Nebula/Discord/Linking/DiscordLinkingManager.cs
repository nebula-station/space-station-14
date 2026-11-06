// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading.Tasks;
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
    private readonly Dictionary<NetUserId, Task<(DiscordLinkingCodeStatus Status, Guid Code)>> _pending = new();
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(3);

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<DiscordLinkingRequestMsg>(OnRequest);
        _net.RegisterNetMessage<DiscordLinkingCodeMsg>();
    }

    private async void OnRequest(DiscordLinkingRequestMsg message)
    {
        var channel = message.MsgChannel;
        var (status, code) = await GetOrCreateCode(channel.UserId);

        if (channel.IsConnected)
            _net.ServerSendMessage(new DiscordLinkingCodeMsg { Status = status, Code = code }, channel);
    }

    public Task<(DiscordLinkingCodeStatus Status, Guid Code)> GetOrCreateCode(NetUserId user)
    {
        if (_activeCodes.TryGetValue(user, out var active) && active.Expires > _timing.RealTime)
            return Task.FromResult((DiscordLinkingCodeStatus.Ok, active.Code));

        if (_pending.TryGetValue(user, out var pending))
            return pending;

        var task = CreateCode(user);
        if (!task.IsCompleted)
            _pending[user] = task;

        return task;
    }

    public Guid? GetActiveCode(NetUserId user)
    {
        return _activeCodes.TryGetValue(user, out var active) && active.Expires > _timing.RealTime
            ? active.Code
            : null;
    }

    private async Task<(DiscordLinkingCodeStatus Status, Guid Code)> CreateCode(NetUserId user)
    {
        try
        {
            var code = Guid.NewGuid();
            var status = await _serverApi.RegisterLinkingCode(user.UserId, code);

            if (status == DiscordLinkingCodeStatus.Ok)
            {
                _activeCodes[user] = (code, _timing.RealTime + CodeLifetime);
                return (status, code);
            }

            _activeCodes.Remove(user);
            return (status, default);
        }
        finally
        {
            _pending.Remove(user);
        }
    }
}
