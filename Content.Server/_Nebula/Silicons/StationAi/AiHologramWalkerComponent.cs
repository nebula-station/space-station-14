// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Nebula.Silicons.StationAi;

/// <summary>
/// AI eye that carries the hologram of the holopad it was projected to.
/// </summary>
[RegisterComponent]
public sealed partial class AiHologramWalkerComponent : Component
{
    [ViewVariables]
    public EntityUid Holopad;

    [ViewVariables]
    public EntityUid Hologram;
}
