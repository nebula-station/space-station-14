// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/dbb6977519b7f974a2dd0e9ca1f4aca27ecc62c9/Content.Shared/_Arcane/Store/Components/UplinkRefundableComponent.cs

using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Shared._Arcane.Store.Components;

[RegisterComponent]
public sealed partial class UplinkRefundableComponent : Component
{
    [DataField]
    public EntityUid OriginalUplink;

    [DataField]
    public FixedPoint2 RefundAmount;

    [DataField]
    public bool IsUsed;

    [DataField]
    public ProtoId<CurrencyPrototype> Currency = "Telecrystal";

    [DataField]
    public List<string>? OriginalContainerPrototypes;

    [DataField]
    public Dictionary<string, FixedPoint2>? OriginalSolutionReagents;
}
