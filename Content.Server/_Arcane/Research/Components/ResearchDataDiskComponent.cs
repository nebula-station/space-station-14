// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/02b6d617ada28d2a78cfd227b9860838fb082909/Content.Arcane.Server/Research/Components/ResearchDataDiskComponent.cs

using Content.Server._Arcane.Research.Systems;

namespace Content.Server._Arcane.Research.Components;

[RegisterComponent, Access(typeof(ResearchDataDiskSystem))]
public sealed partial class ResearchDataDiskComponent : Component
{
    [DataField]
    public bool HasDataSnapshot;

    [DataField]
    public List<string> StoredTechnologies = [];

    /// <summary>
    /// Recipes the network had from technology disks rather than from research.
    /// </summary>
    [DataField]
    public List<string> StoredRecipes = [];

    [DataField]
    public string? SnapshotServerName;
}
