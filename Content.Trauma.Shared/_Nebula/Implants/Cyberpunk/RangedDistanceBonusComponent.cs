// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

/// <summary>
/// Raises the ranged damage of its owner by the distance to the target.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RangedDistanceBonusComponent : Component
{
    [DataField(readOnly: true)]
    public float BonusPerTile = 0.01f;

    [DataField(readOnly: true)]
    public float MaxBonus = 0.3f;
}
