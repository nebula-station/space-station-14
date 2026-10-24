// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Sponsor;

namespace Content.Client._Nebula.Sponsor;

public sealed partial class SponsorSystem : EntitySystem
{
    /// <summary>
    /// Boosty level of the local player, null when not a sponsor.
    /// </summary>
    public int? Rank { get; private set; }

    public event Action? StatusChanged;

    [SubscribeNetworkEvent]
    private void OnStatusResponse(SponsorStatusResponse msg)
    {
        Rank = msg.Rank;
        StatusChanged?.Invoke();
    }

    public void RequestStatus()
    {
        RaiseNetworkEvent(new SponsorStatusRequest());
    }
}
