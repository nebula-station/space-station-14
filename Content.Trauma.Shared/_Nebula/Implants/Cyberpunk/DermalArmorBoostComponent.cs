// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

/// <summary>
/// Makes the protection of the dermal armor in the body stronger.
/// </summary>
[RegisterComponent]
public sealed partial class DermalArmorBoostComponent : Component
{
    [DataField(readOnly: true)]
    public float Multiplier = 1.2f;
}
