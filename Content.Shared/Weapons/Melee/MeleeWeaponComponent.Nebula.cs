// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Weapons.Melee;

public sealed partial class MeleeWeaponComponent
{
    /// <summary>
    /// Limits <see cref="ResistanceBypass"/> to light attacks, wide swings go through armor resistances as usual.
    /// </summary>
    [DataField]
    public bool ResistanceBypassLightOnly;

    /// <summary>
    /// How far from the user the swing and thrust animations reach, in tiles. Ported from Monolith.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AnimationOffset = 1f;

    /// <summary>
    /// Alternates the wide swing direction on every attack, like Monolith does.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AlternateSwing = true;
}
