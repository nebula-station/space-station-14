// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.Implants;

/// <summary>
/// Throat implant that acts as a breath tool, so internals work without a mask.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BreathingTubeImplantComponent : Component;
