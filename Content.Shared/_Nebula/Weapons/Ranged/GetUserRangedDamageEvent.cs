// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Shared._Nebula.Weapons.Ranged;

/// <summary>
/// Raised on the shooter when their projectile or hitscan is about to damage the target, to change that damage.
/// </summary>
[ByRefEvent]
public record struct GetUserRangedDamageEvent(EntityUid Target, DamageSpecifier Damage);
