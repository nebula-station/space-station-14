// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/02b6d617ada28d2a78cfd227b9860838fb082909/Content.Arcane.Server/Objectives/Components/ResearchDiskConditionComponent.cs

using Content.Server._Arcane.Objectives.Systems;

namespace Content.Server._Arcane.Objectives.Components;

[RegisterComponent, Access(typeof(ResearchDiskConditionSystem))]
public sealed partial class ResearchDiskConditionComponent : Component
{
    [DataField]
    public int MinTechnologyCount = 18;

    [DataField]
    public int MaxTechnologyCount = 30;

    [DataField]
    public int RequiredTechnologyCount;

    [DataField(required: true)]
    public LocId ObjectiveText;

    [DataField(required: true)]
    public LocId DescriptionText;
}
