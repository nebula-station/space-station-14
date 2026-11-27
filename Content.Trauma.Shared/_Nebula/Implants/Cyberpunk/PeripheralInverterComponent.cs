// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

/// <summary>
/// Cuts the damage from attackers close to its owner and raises the damage from those further away.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PeripheralInverterComponent : Component
{
    [DataField(readOnly: true)]
    public float Range = 3f;

    [DataField(readOnly: true)]
    public float NearMultiplier = 0.8f;

    [DataField(readOnly: true)]
    public float FarMultiplier = 1.1f;
}
