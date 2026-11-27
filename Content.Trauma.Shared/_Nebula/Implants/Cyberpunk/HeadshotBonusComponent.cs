// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

/// <summary>
/// Raises the damage its owner deals while aiming at the head.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HeadshotBonusComponent : Component
{
    [DataField(readOnly: true)]
    public float Bonus = 0.1f;
}
