// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Shared/_LuaM/Cyberware/CyberpsychosisBlockerComponent.cs

namespace Content.Shared._LuaM.Cyberware;

/// <summary>
/// Put on a station grid. Nobody standing on it can fall into cyberpsychosis.
/// </summary>
[RegisterComponent]
public sealed partial class CyberpsychosisBlockerComponent : Component;
