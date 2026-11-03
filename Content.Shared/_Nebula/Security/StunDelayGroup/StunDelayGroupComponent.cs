// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.Security.StunDelayGroup;

/// <summary>
/// A stunning hit with this item puts its wielder on cooldown, blocking attacks with every item
/// that has this component, so swapping batons doesn't bypass the delay.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StunDelayGroupComponent : Component
{
    /// <summary>
    /// UseDelay id on this item; its length is the wielder's cooldown.
    /// </summary>
    [DataField(readOnly: true)]
    public string UseDelay = "stunbaton";
}
