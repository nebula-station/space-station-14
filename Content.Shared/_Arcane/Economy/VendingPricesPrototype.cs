// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/accb2613b4ea35df58380cc8c7c82cfb2968670a/Content.Arcane.Shared/Economy/VendingPricesPrototype.cs

using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Arcane.Economy;

/// <summary>
///     Prices of a vending machine inventory pack, the ID is the pack ID.
/// </summary>
[Prototype]
public sealed partial class VendingPricesPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Price of regular items without an own price.
    /// </summary>
    [DataField]
    public int DefaultPrice;

    /// <summary>
    ///     Price of contraband and emagged items without an own price.
    /// </summary>
    [DataField]
    public int ExtraPrice;

    [DataField]
    public Dictionary<EntProtoId, int> Prices = [];

    [DataField]
    public Dictionary<EntProtoId, int> ContrabandPrices = [];

    [DataField]
    public Dictionary<EntProtoId, int> EmaggedPrices = [];

    /// <summary>
    ///     Station account that receives the money, the primary station account when null.
    /// </summary>
    [DataField]
    public ProtoId<CargoAccountPrototype>? Account;

    /// <summary>
    ///     Employees of this department pay <see cref="DepartmentDiscount"/> of the price for regular items.
    /// </summary>
    [DataField]
    public ProtoId<CargoAccountPrototype>? DiscountDepartment;

    [DataField]
    public float DepartmentDiscount = 0.2f;
}
