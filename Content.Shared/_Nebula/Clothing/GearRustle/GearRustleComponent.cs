// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.Clothing.GearRustle;

/// <summary>
/// Belt that rustles while its wearer sprints, one collection part per footstep in order.
/// </summary>
[RegisterComponent]
public sealed partial class GearRustleComponent : Component
{
    /// <summary>
    /// Parts of the rustle sound, played in order. Null disables the rustle for an inheriting prototype.
    /// </summary>
    [DataField(readOnly: true)]
    public ProtoId<SoundCollectionPrototype>? Collection = "SecGearRustle";

    [DataField(readOnly: true)]
    public AudioParams Params = AudioParams.Default.WithVolume(-2f);

    [ViewVariables]
    public int NextIndex;
}
