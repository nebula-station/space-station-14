// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Shared/_LuaM/Cyberware/CyberpsychosisGlitchComponent.cs

using Robust.Shared.GameStates;

namespace Content.Shared._LuaM.Cyberware;

/// <summary>
/// Shows the cyberpsychosis glitch shader to the player attached to this entity.
/// It sits on the viewer the player watches through while their body is out of control,
/// and on a user of a running CNS sandevistan.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CyberpsychosisGlitchComponent : Component
{
    /// <summary>
    /// Strength of the glitch, 0 to 1.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Intensity = 1f;
}
