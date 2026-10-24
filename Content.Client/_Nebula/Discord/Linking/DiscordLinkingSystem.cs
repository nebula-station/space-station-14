// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Discord.Linking;

namespace Content.Client._Nebula.Discord.Linking;

public sealed partial class DiscordLinkingSystem : EntitySystem
{
    public bool IsVerified { get; private set; }

    public event Action? VerifiedChanged;

    [SubscribeNetworkEvent]
    private void OnVerifyStatusResponse(VerifyStatusResponse msg)
    {
        IsVerified = msg.Verified;
        VerifiedChanged?.Invoke();
    }

    public void RequestVerificationStatus()
    {
        RaiseNetworkEvent(new VerifyStatusRequest());
    }
}
