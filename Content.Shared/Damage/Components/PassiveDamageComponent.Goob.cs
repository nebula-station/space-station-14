// SPDX-License-Identifier: MIT
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873d/Content.Shared/Damage/Components/PassiveDamageComponent.cs

using Content.Shared.FixedPoint;

namespace Content.Shared.Damage.Components;

public sealed partial class PassiveDamageComponent
{
    /// <summary>
    /// Passive damage stops applying once total damage reaches this value. Zero disables the cap.
    /// </summary>
    [DataField]
    public FixedPoint2 DamageCap;
}
