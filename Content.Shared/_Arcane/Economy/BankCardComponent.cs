// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/accb2613b4ea35df58380cc8c7c82cfb2968670a/Content.Arcane.Shared/Economy/BankCardComponent.cs

using Content.Shared.Cargo.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Arcane.Economy;

/// <summary>
///     ID card that can be linked to a personal bank account. Mirrors the balance and the department of the account
///     so clients can show prices and balances.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BankCardComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? AccountId;

    [DataField, AutoNetworkedField]
    public int Balance;

    /// <summary>
    ///     Department of the account owner, gets discounts at department vending machines.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<CargoAccountPrototype>? Department;
}

[Serializable, NetSerializable]
public enum BankCardUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class BankCardWithdrawMessage(int amount) : BoundUserInterfaceMessage
{
    public readonly int Amount = amount;
}

[Serializable, NetSerializable]
public sealed class BankCardBoundUiState(string? accountId, int balance) : BoundUserInterfaceState
{
    public readonly string? AccountId = accountId;
    public readonly int Balance = balance;
}
