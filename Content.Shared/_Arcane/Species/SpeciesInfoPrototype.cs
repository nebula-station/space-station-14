// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/862852d48934ecca32ab2d9b44eeb3ecf73874a0/Content.Arcane.Shared/Species/SpeciesInfoPrototype.cs

using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Arcane.Species;

/// <summary>
/// What the species window shows for a species (Orion #45). The ID is the species ID.
/// </summary>
[Prototype]
public sealed partial class SpeciesInfoPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public SpeciesCategory Category = SpeciesCategory.Classic;

    /// <summary>
    /// Guidebook-style document shown under the preview. Falls back to the guide entry with the species ID.
    /// </summary>
    [DataField]
    public ResPath? Description;

    [DataField]
    public List<LocId> Pros = [];

    [DataField]
    public List<LocId> Special = [];

    [DataField]
    public List<LocId> Cons = [];
}

public enum SpeciesCategory : byte
{
    Classic,
    Unusual,
    Special,
}
