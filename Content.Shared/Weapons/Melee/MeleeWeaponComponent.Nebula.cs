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
    /// Not networked, it only comes from the prototype and the component is already at the 32 networked fields limit.
    /// </summary>
    [DataField]
    public float AnimationOffset = 1f;

    /// <summary>
    /// Alternates the wide swing direction on every attack, like Monolith does.
    /// Not networked for the same reason as <see cref="AnimationOffset"/>.
    /// </summary>
    [DataField]
    public bool AlternateSwing = true;

    /// <summary>
    /// Current side of an alternating swing, toggled every attack when <see cref="AlternateSwing"/> is set.
    /// </summary>
    [AutoNetworkedField]
    public bool SwingFlipped;
}
