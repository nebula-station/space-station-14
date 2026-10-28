// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Discord.Linking;
using Robust.Shared.Network;

namespace Content.Client._Nebula.Discord.Linking;

public sealed partial class DiscordLinkingManager : IPostInjectInit
{
    [Dependency] private INetManager _net = default!;

    public event Action<DiscordLinkingCodeStatus, Guid>? CodeReceived;

    private void OnCode(DiscordLinkingCodeMsg message)
    {
        CodeReceived?.Invoke(message.Status, message.Code);
    }

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<DiscordLinkingCodeMsg>(OnCode);
        _net.RegisterNetMessage<DiscordLinkingRequestMsg>();
    }
}
