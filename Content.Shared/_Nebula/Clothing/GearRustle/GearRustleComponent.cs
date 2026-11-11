// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Shared._Nebula.Clothing.GearRustle;

/// <summary>
/// Belt that rustles on each footstep while its wearer sprints.
/// </summary>
[RegisterComponent]
public sealed partial class GearRustleComponent : Component
{
    /// <summary>
    /// Null disables the rustle for an inheriting prototype.
    /// </summary>
    [DataField(readOnly: true)]
    public SoundSpecifier? Sound = new SoundCollectionSpecifier("SecGearRustle", AudioParams.Default.WithVolume(-12.7f));
}
