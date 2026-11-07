// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Server/_Orion/Doors/Components/MultiTileAirlockAirtightComponent.cs

namespace Content.Server._Orion.Doors.Components;

/// <summary>
/// Marks a multi-tile airlock so that a phantom airtight entity is spawned
/// on each extra tile, sealing atmosphere on all of them when closed.
/// </summary>
[RegisterComponent]
public sealed partial class MultiTileAirlockAirtightComponent : Component
{
    /// <summary>
    /// Local-space offsets (pre-rotation) of additional tiles to seal.
    /// </summary>
    [DataField]
    public List<Vector2i> TileOffsets = [new(1, 0)];

    [ViewVariables]
    public List<EntityUid> PhantomEntities = [];
}
