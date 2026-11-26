// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/accb2613b4ea35df58380cc8c7c82cfb2968670a/Content.Arcane.Server/Economy/BankAccountComponent.cs

using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Arcane.Economy;

/// <summary>
///     Personal bank account, kept on the mind so it follows the character.
/// </summary>
[RegisterComponent, Access(typeof(BankSystem))]
public sealed partial class BankAccountComponent : Component
{
    [DataField]
    public string AccountId = string.Empty;

    [DataField]
    public string OwnerName = string.Empty;

    [DataField]
    public int Balance;

    /// <summary>
    ///     Job the salary is paid for.
    /// </summary>
    [DataField]
    public string? JobId;

    /// <summary>
    ///     Station paying the salary, used while the owner is away from it.
    /// </summary>
    [DataField]
    public EntityUid? Station;
}
