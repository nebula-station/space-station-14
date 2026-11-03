// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.Sponsor;

/// <summary>
/// A Boosty subscription level as described to the players in the support window.
/// This is only the text, the bonuses themselves are checked in their own systems.
/// </summary>
[Prototype]
public sealed partial class SponsorTierPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The Boosty rank of this level.
    /// </summary>
    [DataField(required: true)]
    public int Rank;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public List<LocId> Perks = [];
}
