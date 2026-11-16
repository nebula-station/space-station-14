// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.Decals;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Random;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCUtilitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityQuery<DecalGridComponent> _decalGridQuery = default!;

    /// <summary>
    /// Reads decals from the grid's decal chunks, an entity lookup would visit every entity in range.
    /// </summary>
    private void AddCleanableDecals(EntityUid owner, float range, HashSet<EntityUid> entities)
    {
        var xform = Transform(owner);
        if (xform.GridUid is not { } grid || !_decalGridQuery.TryComp(grid, out var decalGrid))
            return;

        var pos = Vector2.Transform(_transform.GetWorldPosition(xform), _transform.GetInvWorldMatrix(grid));
        var chunks = new ChunkIndicesEnumerator(pos, range, SharedDecalSystem.ChunkSize);
        var rangeSquared = range * range;

        while (chunks.MoveNext(out var indices))
        {
            if (!decalGrid.ChunkCollection.ChunkCollection.TryGetValue(indices.Value, out var chunk))
                continue;

            foreach (var decal in chunk.Decals)
            {
                if (decal.Cleanable && (decal.Coordinates - pos).LengthSquared() <= rangeSquared)
                    entities.Add(decal.Ent);
            }
        }
    }
}
