// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.Squads;

/// <summary>
/// Marks the body of a security squad leader, the first member of the squad. Security HUDs show an icon over it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SquadLeaderComponent : Component;
