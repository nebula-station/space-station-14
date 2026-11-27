// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/dfbda555db9acabf3827cf3b0785c6deaa5c1a8b/Content.Server/GameTicking/Events/PlayerJoinRequestedEvent.Arcane.cs

using Robust.Shared.Player;

namespace Content.Server.GameTicking.Events;

/// <summary>
/// Content may defer entry after connection checks, before player data starts loading.
/// </summary>
[ByRefEvent]
public struct PlayerJoinRequestedEvent(ICommonSession session)
{
    public readonly ICommonSession Session = session;
    public bool Deferred;
}
