// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.Examine;

/// <summary>
/// Lists the clothing worn by this character when examined up close.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ExaminableCharacterComponent : Component;
