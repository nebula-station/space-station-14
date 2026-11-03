// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873d/Content.Server/_DV/Feroxi/FeroxiDehydrateComponent.cs

using Content.Shared.Metabolism;
using Robust.Shared.Prototypes;

namespace Content.Server._DV.Feroxi;

/// <summary>
/// Swaps the metabolizer of the lungs depending on thirst, so a dehydrated Feroxi can only breathe water vapor.
/// </summary>
[RegisterComponent, Access(typeof(FeroxiDehydrateSystem))]
public sealed partial class FeroxiDehydrateComponent : Component
{
    /// <summary>
    /// Metabolizer of the lungs while thirst is above <see cref="DehydrationThreshold"/>.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MetabolizerTypePrototype> HydratedMetabolizer;

    /// <summary>
    /// Metabolizer of the lungs while thirst is at or below <see cref="DehydrationThreshold"/>.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MetabolizerTypePrototype> DehydratedMetabolizer;

    [DataField]
    public bool Dehydrated;

    [DataField(required: true)]
    public float DehydrationThreshold;
}
