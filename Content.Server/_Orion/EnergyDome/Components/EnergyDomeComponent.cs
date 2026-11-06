// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Server/_Orion/EnergyDome/Components/EnergyDomeComponent.cs

using Content.Server._Orion.EnergyDome.Systems;

namespace Content.Server._Orion.EnergyDome.Components;

/// <summary>
/// Allows linking the dome generator with the dome itself
/// </summary>
[RegisterComponent, Access(typeof(EnergyDomeSystem))]
public sealed partial class EnergyDomeComponent : Component
{
    /// <summary>
    /// Linked generator that uses energy
    /// </summary>
    [DataField]
    public EntityUid? Generator;
}
