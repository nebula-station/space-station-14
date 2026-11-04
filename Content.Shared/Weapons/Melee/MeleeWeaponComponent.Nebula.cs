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
}
