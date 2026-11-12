// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared._Nebula.ErpChangeling;

/// <summary>
/// A changeling without combat abilities that can only sting or ERP with characters who agreed to be horny antag victims.
/// It evolves by bringing partners to orgasm instead of absorbing them.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ErpChangelingComponent : Component
{
    /// <summary>
    /// Evolution points given the first time each partner orgasms from our interactions.
    /// </summary>
    [DataField]
    public float EvolutionPointsPerPartner = 2f;

    /// <summary>
    /// How long after our last interaction a partner's orgasm still counts as ours.
    /// </summary>
    [DataField]
    public TimeSpan PartnerWindow = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Minds, or bodies for NPCs, of partners that already gave evolution points.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> RewardedPartners = new();
}
