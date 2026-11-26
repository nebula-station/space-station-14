// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Shared/PDA/PdaScreenComponent.cs

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Arcane.PDA;

/// <summary>
/// Shows what the PDA is doing on its screen: off, the menu or the active program.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PdaScreenComponent : Component
{
    /// <summary>
    /// Screen state per program prototype.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, string> ProgramScreens = [];

    [DataField]
    public string OffState = "pda_screen_borders";

    [DataField]
    public string MenuState = "menu";

    /// <summary>
    /// Shown for programs without their own screen.
    /// </summary>
    [DataField]
    public string ProgramState = "generic";
}

[Serializable, NetSerializable]
public enum PdaScreenVisuals : byte
{
    State,
}

public enum PdaScreenLayers : byte
{
    Screen,
}
