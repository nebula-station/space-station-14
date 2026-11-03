// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.MartialArts;
using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.Movement.Pulling;

/// <summary>
/// Gloves that start a combat mode pull at this grab stage instead of a soft grab.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StartingGrabStageComponent : Component
{
    [DataField]
    public GrabStage Stage = GrabStage.Soft;
}
