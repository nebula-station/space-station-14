// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

/// <summary>
/// Action of cyberware, so implants that change cooldowns can find it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ImplantActionComponent : Component;
