// SPDX-License-Identifier: AGPL-3.0-or-later

// Ported from Radiant Sector: https://github.com/TetrisZz/frontier-Radiant

using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.Leash;

// Nebula start
/// <summary>
/// Added to a mob while it is on someone's leash.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LeashedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Puller;

    /// <summary>
    /// Physics anchor (<c>TetherEntity</c>) the mob is mouse-jointed to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Anchor;

    /// <summary>
    /// Current leash length.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Distance = 3f;
}
// Nebula end
