// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;

namespace Content.Shared._Nebula.Summoning;

/// <summary>
/// Entity currently summoned to a rune and leashed to it.
/// </summary>
[RegisterComponent]
public sealed partial class SummonedComponent : Component
{
    [ViewVariables]
    public EntityUid Rune;

    [ViewVariables]
    public EntityCoordinates ReturnCoordinates;

    [ViewVariables]
    public EntityUid? ReturnStation;
}
