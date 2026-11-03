// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Nebula.Chemistry;

/// <summary>
/// Raised on a melee, projectile or embed injector before it injects into each target.
/// The system cancelling it shows the popup itself.
/// </summary>
[ByRefEvent]
public record struct InjectOnEventAttemptEvent(EntityUid Target, EntityUid? User, bool Cancelled = false);
