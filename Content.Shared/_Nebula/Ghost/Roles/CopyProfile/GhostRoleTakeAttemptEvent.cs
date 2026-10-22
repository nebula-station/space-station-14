// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Player;

namespace Content.Shared._Nebula.Ghost.Roles.CopyProfile;

[ByRefEvent]
public record struct GhostRoleTakeAttemptEvent(ICommonSession Player)
{
    public bool Cancelled;
}
