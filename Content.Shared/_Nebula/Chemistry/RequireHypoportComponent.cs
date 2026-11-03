// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.Chemistry;

/// <summary>
/// On-hit injector that only injects into targets with an accessible hypoport.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RequireHypoportComponent : Component;
