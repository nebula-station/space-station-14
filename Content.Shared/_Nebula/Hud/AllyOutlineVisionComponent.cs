// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.Hud;

/// <summary>
/// Night vision that outlines the wearer's squad while it is on.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AllyOutlineVisionComponent : Component;
