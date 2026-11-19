// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Nebula.Damage;

/// <summary>
/// Raised on an entity right after it enters a full stamina crit.
/// </summary>
[ByRefEvent]
public record struct StaminaCritEnteredEvent;
