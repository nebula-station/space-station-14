// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.Sponsor;

[Serializable, NetSerializable]
public sealed class SponsorStatusRequest : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class SponsorStatusResponse : EntityEventArgs
{
    /// <summary>
    /// Boosty level, null when the player is not a sponsor.
    /// </summary>
    public int? Rank;
}
