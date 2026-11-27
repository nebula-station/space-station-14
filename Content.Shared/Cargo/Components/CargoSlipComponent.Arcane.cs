// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/accb2613b4ea35df58380cc8c7c82cfb2968670a/Content.Shared/Cargo/Components/CargoSlipComponent.Arcane.cs

namespace Content.Shared.Cargo.Components;

/// <summary>
/// Arcane - order slip fields from Orion #377.
/// </summary>
public sealed partial class CargoSlipComponent
{
    [DataField]
    public string? DeliveryDestination;

    [DataField]
    public bool SecuredDelivery;

    [DataField]
    public int ExtraCost;
}
