// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

/// <summary>
/// Cuts the remaining cooldowns of the body's cyberware each time it puts someone into crit.
/// </summary>
[RegisterComponent]
public sealed partial class CritCooldownRefundComponent : Component
{
    /// <summary>
    /// Part of the remaining cooldown taken off.
    /// </summary>
    [DataField(readOnly: true)]
    public float Fraction = 0.05f;
}
