// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.ErpPanel.Requirements;
using Content.Shared.Dataset;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Nebula.ErpPanel;

public enum PanelInteractionKind : byte
{
    Soft,
    Erotic,
    Harmful,
    HarmfulErotic,
}

[Prototype]
public sealed partial class PanelInteractionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<PanelInteractionCategoryPrototype> Category;

    [DataField(required: true)]
    public PanelInteractionKind Kind;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? Messages;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? SelfMessages;

    [DataField(required: true)]
    public SpriteSpecifier Icon = SpriteSpecifier.Invalid;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public float Range = 1f;

    [DataField]
    public int UserArouse = 0;

    [DataField]
    public int TargetArouse = 0;

    [DataField]
    public List<ErpRequirement>? UserRequirements;

    [DataField]
    public List<ErpRequirement>? TargetRequirements;

    public bool IsErotic => Kind is PanelInteractionKind.Erotic or PanelInteractionKind.HarmfulErotic;
}

[Prototype]
public sealed partial class PanelInteractionCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public SpriteSpecifier Icon = SpriteSpecifier.Invalid;
}

/// <summary>
/// A sound a player may pick for their custom emote.
/// </summary>
[Prototype]
public sealed partial class PanelCustomSoundPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SoundSpecifier Sound = default!;
}
