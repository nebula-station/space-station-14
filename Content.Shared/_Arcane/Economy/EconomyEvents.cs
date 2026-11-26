// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/accb2613b4ea35df58380cc8c7c82cfb2968670a/Content.Arcane.Common/Economy/EconomyEvents.cs

namespace Content.Shared._Arcane.Economy;

/// <summary>
///     Raised on a vending machine to get the price the user pays for an item.
///     <see cref="InventoryType"/> is the numeric value of the vending inventory type.
/// </summary>
[ByRefEvent]
public record struct VendingItemPriceEvent(EntityUid? User, string ItemId, int InventoryType, int Price = 0);

/// <summary>
///     Raised on a vending machine before it ejects a bought item, cancel it when the payment fails.
/// </summary>
[ByRefEvent]
public record struct VendingPurchaseAttemptEvent(EntityUid? User, string ItemId, int InventoryType, bool Cancelled = false);

/// <summary>
///     Raised broadcast to get the personal bank balance of a user, null when they have no linked card.
/// </summary>
[ByRefEvent]
public record struct BankBalanceQueryEvent(EntityUid User, int? Balance = null);

/// <summary>
///     Raised broadcast to find the bank account linked to the ID of a user.
/// </summary>
[ByRefEvent]
public record struct BankAccountLookupEvent(EntityUid User, string? AccountId = null, string? OwnerName = null);

/// <summary>
///     Raised broadcast to move credits on a personal bank account, negative amounts withdraw.
/// </summary>
[ByRefEvent]
public record struct BankAccountChangeEvent(string AccountId, int Amount, string Reason, bool Handled = false);
