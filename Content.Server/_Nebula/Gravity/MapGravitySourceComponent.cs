// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Nebula.Gravity;

/// <summary>
/// Gives the whole map this entity spawns on permanent gravity, while at least one such entity remains on it.
/// </summary>
[RegisterComponent]
public sealed partial class MapGravitySourceComponent : Component
{
    /// <summary>
    /// Map this entity turned gravity on for. Null if the map already had its own gravity.
    /// </summary>
    [ViewVariables]
    public EntityUid? GrantedMap;
}
