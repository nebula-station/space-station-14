// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Nebula.Implants;

/// <summary>
/// Raised on cyberware with its own timer when it starts a new cooldown, so the cooldown modifiers of the body apply.
/// </summary>
[ByRefEvent]
public record struct GetImplantCooldownEvent(EntityUid Body, TimeSpan Cooldown);
