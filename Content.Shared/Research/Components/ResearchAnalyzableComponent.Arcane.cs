// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/02b6d617ada28d2a78cfd227b9860838fb082909/Content.Shared/Research/Components/ResearchAnalyzableComponent.Arcane.cs

namespace Content.Shared.Research.Components;

[RegisterComponent]
public sealed partial class ResearchAnalyzableComponent : Component
{
    [DataField]
    public Dictionary<string, List<ResearchPointAmount>> MethodPointRewards = [];

    [DataField]
    public List<string> SupportedMethods = [];

    [DataField]
    public List<string> UnlockTechnologies = [];

    [DataField]
    public List<string> RevealTechnologies = [];

    [DataField]
    public string? DiscoveryTrigger;

    [DataField]
    public List<string> ExperimentActions = [];
}
