// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

/// <summary>
/// Raised on cyberware with its own timer to check whether it is recharging or spent.
/// </summary>
[ByRefEvent]
public record struct ImplantCooldownQueryEvent(bool OnCooldown = false);

/// <summary>
/// Raised on cyberware with its own timer to take this part off its remaining cooldown.
/// </summary>
[ByRefEvent]
public record struct ImplantCooldownReduceEvent(float Fraction);

/// <summary>
/// Raised on cyberware with its own timer to make it ready again.
/// </summary>
[ByRefEvent]
public record struct ImplantCooldownResetEvent;
