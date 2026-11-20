// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/TetrisZz/frontier-Radiant/blob/743dfcac03/Content.Server/_Starlight/HoloItem/HoloItemComponent.cs

using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.HoloItem;

/// <summary>
/// Projects a temporary holographic item and uses it on the clicked target, spending power cell charge.
/// </summary>
[RegisterComponent]
public sealed partial class HoloItemComponent : Component
{
    [DataField(required: true)]
    public EntProtoId ItemPrototype;

    [DataField]
    public float ChargeUse = 50f;

    /// <summary>
    /// Components the target must have for the projection to happen.
    /// </summary>
    [DataField]
    public ComponentRegistry RequiredComponents = new();
}
