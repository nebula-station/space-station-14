// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

/// <summary>
/// Scales every new cooldown of the cyberware in this body, except the implant giving it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ImplantCooldownModifierComponent : Component
{
    [DataField(readOnly: true)]
    public float Multiplier = 0.85f;
}
