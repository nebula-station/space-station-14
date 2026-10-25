// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/02837fb57c50d53cddaeab30fe0e12477e02efda/Content.Shared/_Orion/Sprite/EdgeConnections/EdgeConnectionComponent.cs

using Robust.Shared.Utility;

namespace Content.Shared._Orion.Sprite.EdgeConnections;

/// <summary>
/// Marks entities that should visually connect to adjacent peers with the same key.
/// </summary>
[RegisterComponent]
public sealed partial class EdgeConnectionComponent : Component
{
    /// <summary>
    /// Only entities with equal keys can form a visual connection.
    /// </summary>
    [DataField]
    public string ConnectionKey = "default";

    /// <summary>
    /// Relative directions this entity can connect in.
    /// These directions are rotated by the entity transform.
    /// </summary>
    [DataField]
    public EdgeConnectionDirections AllowedDirections = EdgeConnectionDirections.None;

    /// <summary>
    /// Sprite layer whose state is swapped by the connection mask.
    /// </summary>
    [DataField]
    public string Layer = "computerLayerBody";

    /// <summary>
    /// RSI and state the layer must show for the connection sprites to apply.
    /// Entities with a different body sprite keep it untouched.
    /// </summary>
    [DataField]
    public ResPath BaseSprite = new("Structures/Machines/computers.rsi");

    /// <inheritdoc cref="BaseSprite"/>
    [DataField]
    public string BaseState = "computer";

    /// <summary>
    /// RSI holding the connected states.
    /// </summary>
    [DataField]
    public ResPath? Sprite;

    /// <summary>
    /// Layer state for each local connection mask. Masks without an entry show the base state.
    /// </summary>
    [DataField]
    public Dictionary<EdgeConnectionDirections, string> States = [];

    /// <summary>
    /// Whether the layer showed the base sprite when first checked on the client.
    /// </summary>
    [ViewVariables]
    public bool? Supported;
}
