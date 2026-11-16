// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Nebula.Projectiles;

/// <summary>
/// Raised on an embeddable projectile before it embeds into <see cref="Target"/>.
/// If cancelled the projectile does not embed and keeps its normal physics.
/// </summary>
[ByRefEvent]
public record struct EmbedAttemptEvent(EntityUid Target, EntityUid? User, bool Cancelled = false);
