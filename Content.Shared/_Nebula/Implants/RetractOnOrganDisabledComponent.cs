// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.Implants;

/// <summary>
/// Organ whose retractable item actions put their item back when the organ is disabled or removed.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RetractOnOrganDisabledComponent : Component;
