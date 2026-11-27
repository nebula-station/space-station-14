// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/accb2613b4ea35df58380cc8c7c82cfb2968670a/Content.Shared/Cargo/Prototypes/CargoAccountPrototype.Arcane.cs

using Content.Shared.Access;
using Robust.Shared.Prototypes;

namespace Content.Shared.Cargo.Prototypes;

/// <summary>
/// Arcane - department delivery settings (Orion #377).
/// </summary>
public sealed partial class CargoAccountPrototype
{
    /// <summary>
    /// Default delivery destination shown on this account's orders.
    /// </summary>
    [DataField]
    public LocId? DepartmentDestinationName;

    /// <summary>
    /// Secure crate secured orders of this account are delivered in.
    /// </summary>
    [DataField]
    public EntProtoId? SecureCratePrototype;

    /// <summary>
    /// Access of this department; locked crates it can't open are never secured-delivered.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<AccessLevelPrototype>> SecureCrateOrderAccess = [];
}
