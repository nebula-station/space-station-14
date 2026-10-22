// SPDX-License-Identifier: AGPL-3.0-or-later

// Ported from Radiant Sector: https://github.com/TetrisZz/frontier-Radiant

using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.Leash;

/// <summary>
/// Added to a mob while it holds a leash attached to someone.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LeashPullerComponent : Component
{
    // Nebula start
    /// <summary>
    /// The held leash item; the leash snaps if it leaves the puller's hands.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Leash;

    [DataField, AutoNetworkedField]
    public List<EntityUid> Targets = new();

    [DataField, AutoNetworkedField]
    public TimeSpan NextYank;
    // Nebula end
}
