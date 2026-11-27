// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Shared/PDA/PdaSkinPrototype.cs

using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Arcane.PDA;

/// <summary>
/// Colored look of a PDA. The id is the PDA entity prototype; PDAs without a skin use their closest parent's.
/// </summary>
[Prototype]
public sealed partial class PdaSkinPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// RSI with the skin states.
    /// </summary>
    [DataField]
    public ResPath Sprite = new("/Textures/_Arcane/Objects/Devices/pda.rsi");

    [DataField]
    public string BaseState = "pda_base";

    [DataField]
    public Color? BaseColor;

    /// <summary>
    /// Layers drawn over the base, below the screen.
    /// </summary>
    [DataField]
    public List<PdaSkinLayer> Layers = [];

    /// <summary>
    /// Overrides the PDA interface border colors.
    /// </summary>
    [DataField]
    public Color? BorderColor;

    [DataField]
    public Color? AccentColor;
}

[DataDefinition]
public sealed partial class PdaSkinLayer
{
    [DataField(required: true)]
    public string State = string.Empty;

    [DataField]
    public Color? Color;
}
