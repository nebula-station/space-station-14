// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid.Prototypes;

public sealed partial class SpeciesPrototype
{
    // Nebula start

    /// <summary>
    ///     The minimum height and width ratio for this species
    /// </summary>
    [DataField]
    public float SizeRatio = 1.2f;

    /// <summary>
    ///     The minimum height for this species
    /// </summary>
    [DataField]
    public float MinHeight = 0.80f;

    /// <summary>
    ///     The default height for this species
    /// </summary>
    [DataField]
    public float DefaultHeight = 1f;

    /// <summary>
    ///     The maximum height for this species
    /// </summary>
    [DataField]
    public float MaxHeight = 1.20f;

    /// <summary>
    ///     The minimum width for this species
    /// </summary>
    [DataField]
    public float MinWidth = 0.85f;

    /// <summary>
    ///     The default width for this species
    /// </summary>
    [DataField]
    public float DefaultWidth = 1f;

    /// <summary>
    ///     The maximum width for this species
    /// </summary>
    [DataField]
    public float MaxWidth = 1.15f;

    /// <summary>
    ///     The average height in centimeters for this species, used to calculate player facing height values in UI elements
    /// </summary>
    [DataField]
    public float AverageHeight = 176.1f;

    /// <summary>
    ///     The average shoulder-to-shoulder width in cm for this species, used to calculate player facing width values in UI elements
    /// </summary>
    [DataField]
    public float AverageWidth = 40f;

    // Nebula end
}
