// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Server/_Orion/Doors/Systems/MultiTileAirlockAirtightSystem.cs

using Content.Server._Orion.Doors.Components;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.Doors.Systems;

public sealed partial class MultiTileAirlockAirtightSystem : EntitySystem
{
    [Dependency] private AirtightSystem _airtight = default!;
    [Dependency] private SharedMapSystem _map = default!;

    private static readonly EntProtoId PhantomPrototype = "PhantomAirtight";

    [SubscribeLocalEvent]
    private void OnAnchorChanged(Entity<MultiTileAirlockAirtightComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            SpawnPhantoms(ent);
        else
            DeletePhantoms(ent);
    }

    [SubscribeLocalEvent]
    private void OnMoved(Entity<MultiTileAirlockAirtightComponent> ent, ref MoveEvent args)
    {
        if (args.OldPosition == args.NewPosition && args.OldRotation == args.NewRotation)
            return;

        if (args.Component.Anchored)
            SpawnPhantoms(ent);
        else
            DeletePhantoms(ent);
    }

    [SubscribeLocalEvent]
    private void OnAirtightChanged(Entity<MultiTileAirlockAirtightComponent> ent, ref AirtightChanged args)
    {
        foreach (var phantom in ent.Comp.PhantomEntities)
        {
            if (TryComp<AirtightComponent>(phantom, out var phantomAirtight))
                _airtight.SetAirblocked((phantom, phantomAirtight), args.Airtight.AirBlocked);
        }
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<MultiTileAirlockAirtightComponent> ent, ref ComponentShutdown args)
    {
        DeletePhantoms(ent);
    }

    private void SpawnPhantoms(Entity<MultiTileAirlockAirtightComponent> ent)
    {
        DeletePhantoms(ent);

        var xform = Transform(ent);
        if (!xform.Anchored || xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        if (!TryComp<AirtightComponent>(ent, out var parentAirtight))
            return;

        var anchorTile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);

        foreach (var offset in ent.Comp.TileOffsets)
        {
            var rotated = xform.LocalRotation.RotateVec(offset);
            var tile = anchorTile + new Vector2i((int) MathF.Round(rotated.X), (int) MathF.Round(rotated.Y));
            var phantom = Spawn(PhantomPrototype, _map.GridTileToLocal(gridUid, grid, tile));

            if (TryComp<AirtightComponent>(phantom, out var phantomAirtight))
                _airtight.SetAirblocked((phantom, phantomAirtight), parentAirtight.AirBlocked);

            ent.Comp.PhantomEntities.Add(phantom);
        }
    }

    private void DeletePhantoms(Entity<MultiTileAirlockAirtightComponent> ent)
    {
        foreach (var phantom in ent.Comp.PhantomEntities)
        {
            if (!TerminatingOrDeleted(phantom))
                QueueDel(phantom);
        }

        ent.Comp.PhantomEntities.Clear();
    }
}
