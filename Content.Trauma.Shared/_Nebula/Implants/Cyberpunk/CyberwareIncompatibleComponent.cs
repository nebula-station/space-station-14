// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

/// <summary>
/// Cyberware that can't be installed next to the listed organs or implants, checked both ways.
/// </summary>
[RegisterComponent]
public sealed partial class CyberwareIncompatibleComponent : Component
{
    [DataField(readOnly: true)]
    public List<EntProtoId> Incompatible = [];

    /// <summary>
    /// Whether only a body with a mindshield can take this cyberware.
    /// </summary>
    [DataField(readOnly: true)]
    public bool RequireMindShield;
}
