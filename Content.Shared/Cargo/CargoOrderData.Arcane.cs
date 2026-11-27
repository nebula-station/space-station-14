// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/accb2613b4ea35df58380cc8c7c82cfb2968670a/Content.Shared/Cargo/CargoOrderData.Arcane.cs

namespace Content.Shared.Cargo;

/// <summary>
/// Arcane - delivery destination and secured department crate delivery (Orion #377).
/// <see cref="Reason"/> is shown as the order note.
/// </summary>
public sealed partial class CargoOrderData
{
    /// <summary>
    /// Where on the station the order should be delivered, free text.
    /// </summary>
    [DataField]
    public string? DeliveryDestination;

    /// <summary>
    /// Ship the product inside the ordering account's secure crate.
    /// </summary>
    [DataField]
    public bool SecuredDelivery;

    /// <summary>
    /// Flat price added on top of the products, e.g. for secured delivery.
    /// </summary>
    [DataField]
    public int ExtraCost;

    /// <summary>
    /// Personal bank account paying for the order instead of the department.
    /// </summary>
    [DataField]
    public string? PrivateAccountId;
}
