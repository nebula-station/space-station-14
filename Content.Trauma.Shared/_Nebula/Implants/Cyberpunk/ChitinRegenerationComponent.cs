// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;

namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

/// <summary>
/// Slowly heals its owner while the total damage is below a threshold.
/// </summary>
[RegisterComponent]
public sealed partial class ChitinRegenerationComponent : Component
{
    [DataField(readOnly: true)]
    public FixedPoint2 HealPerSecond = 1.5;

    [DataField(readOnly: true)]
    public ProtoId<DamageGroupPrototype> Group = "Brute";

    /// <summary>
    /// No healing once the total damage reaches this.
    /// </summary>
    [DataField(readOnly: true)]
    public FixedPoint2 Threshold = 50;
}
