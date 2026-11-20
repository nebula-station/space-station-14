// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid.Markings;

public sealed partial class MarkingsLimits
{
    /// <summary>
    /// Markings that can't be used on this layer by this group, even though they have no group whitelist.
    /// Profiles that already have them lose them on validation.
    /// For example vulpkanin heads already draw a snout, so the full snout marking would double it.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<MarkingPrototype>> Blacklist = new();
}
