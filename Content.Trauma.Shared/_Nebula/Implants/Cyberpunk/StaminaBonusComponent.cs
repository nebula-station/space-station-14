// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

/// <summary>
/// Raises the stamina crit threshold of its owner.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StaminaBonusComponent : Component
{
    [DataField(readOnly: true)]
    public float Bonus = 25f;
}
