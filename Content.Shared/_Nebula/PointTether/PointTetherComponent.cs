// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.PointTether;

/// <summary>
/// Keeps an entity within range of an anchor. Leaving the range raises <see cref="PointTetherExceededEvent"/>,
/// and the subscriber decides what happens.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class PointTetherComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid Anchor;

    [ViewVariables, AutoNetworkedField]
    public float Range;

    [ViewVariables]
    public TimeSpan CheckInterval;

    [ViewVariables, AutoPausedField]
    public TimeSpan NextCheck;
}

[ByRefEvent]
public readonly record struct PointTetherExceededEvent;
