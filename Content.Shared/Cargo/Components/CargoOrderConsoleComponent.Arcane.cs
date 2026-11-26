// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/accb2613b4ea35df58380cc8c7c82cfb2968670a/Content.Shared/Cargo/Components/CargoOrderConsoleComponent.Arcane.cs

using Robust.Shared.GameStates;

namespace Content.Shared.Cargo.Components;

/// <summary>
/// Arcane - requester names and secured delivery (Orion #377).
/// </summary>
public sealed partial class CargoOrderConsoleComponent
{
    /// <summary>
    /// Extra price for delivering an order inside the account's secure crate.
    /// </summary>
    [DataField]
    public int SecureOrderCost = 100;

    /// <summary>
    /// Whether the requester name can be typed in instead of read from the orderer's ID. Set by emagging.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool EditableRequesterName;
}
