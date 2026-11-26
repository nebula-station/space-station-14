// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

/// <summary>
/// Implant whose next cooldown lasts twice as long, after the quantum regulator reset it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ImplantDoubleCooldownComponent : Component;
