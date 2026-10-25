// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/02837fb57c50d53cddaeab30fe0e12477e02efda/Content.Server/_Orion/Sprite/EdgeConnections/EdgeConnectionSystem.cs

using Content.Shared._Orion.Sprite.EdgeConnections;
using Robust.Shared.Map.Components;

namespace Content.Server._Orion.Sprite.EdgeConnections;

/// <summary>
/// Calculates edge-connection masks for anchored entities on grids.
/// </summary>
public sealed partial class EdgeConnectionSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedMapSystem _map = default!;

    private static readonly (Vector2i Offset, EdgeConnectionDirections Direction, EdgeConnectionDirections Opposite)[] CardinalOffsets =
    [
        (new Vector2i(1, 0), EdgeConnectionDirections.East, EdgeConnectionDirections.West),
        (new Vector2i(-1, 0), EdgeConnectionDirections.West, EdgeConnectionDirections.East),
        (new Vector2i(0, 1), EdgeConnectionDirections.North, EdgeConnectionDirections.South),
        (new Vector2i(0, -1), EdgeConnectionDirections.South, EdgeConnectionDirections.North),
    ];

    private EntityQuery<EdgeConnectionComponent> _edgeQuery;
    private EntityQuery<MapGridComponent> _gridQuery;

    public override void Initialize()
    {
        base.Initialize();

        _edgeQuery = GetEntityQuery<EdgeConnectionComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();
    }

    [SubscribeLocalEvent]
    private void OnConnectionStartup(Entity<EdgeConnectionComponent> ent, ref ComponentStartup args)
    {
        RecalculateEntity(ent);
        RecalculateNeighbors(Transform(ent));
    }

    [SubscribeLocalEvent]
    private void OnAnchorChanged(Entity<EdgeConnectionComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Detaching || !args.Anchored)
            _appearance.SetData(ent, EdgeConnectionVisuals.ConnectionMask, EdgeConnectionDirections.None);
        else
            RecalculateEntity(ent);

        RecalculateNeighbors(args.Transform, ent);
    }

    [SubscribeLocalEvent]
    private void OnConnectionShutdown(Entity<EdgeConnectionComponent> ent, ref ComponentShutdown args)
    {
        _appearance.SetData(ent, EdgeConnectionVisuals.ConnectionMask, EdgeConnectionDirections.None);
        RecalculateNeighbors(Transform(ent), ent);
    }

    [SubscribeLocalEvent]
    private void OnConnectionMoved(Entity<EdgeConnectionComponent> ent, ref MoveEvent args)
    {
        if (!args.Component.Anchored)
            return;

        var movedToNewTile = args.OldPosition.EntityId != args.NewPosition.EntityId
                            || args.OldPosition.Position.Floored() != args.NewPosition.Position.Floored();

        if (!movedToNewTile && args.OldRotation.EqualsApprox(args.NewRotation))
            return;

        RecalculateEntity(ent);
        RecalculateNeighbors(args.Component);
    }

    private void RecalculateEntity(Entity<EdgeConnectionComponent> ent)
    {
        var xform = Transform(ent);

        if (!TryGetGridTile(xform, out var gridUid, out var grid, out var tile))
        {
            _appearance.SetData(ent, EdgeConnectionVisuals.ConnectionMask, EdgeConnectionDirections.None);
            return;
        }

        var worldAllowed = RotateDirections(ent.Comp.AllowedDirections, xform.LocalRotation, clockwise: true);
        var mask = EdgeConnectionDirections.None;

        foreach (var (offset, direction, opposite) in CardinalOffsets)
        {
            if ((worldAllowed & direction) == 0)
                continue;

            if (HasMatchingNeighbor(ent, xform.LocalRotation, gridUid, grid, tile + offset, ent.Comp.ConnectionKey, opposite))
                mask |= direction;
        }

        var localMask = RotateDirections(mask, xform.LocalRotation, clockwise: false);

        if (GetQuarterTurns(xform.LocalRotation) % 2 != 0)
            localMask = FlipEastWest(localMask);

        _appearance.SetData(ent, EdgeConnectionVisuals.ConnectionMask, localMask);
    }

    private bool HasMatchingNeighbor(EntityUid self, Angle selfLocalRotation, EntityUid gridUid, MapGridComponent grid, Vector2i tile, string key, EdgeConnectionDirections requiredDirection)
    {
        var selfQuarterTurns = GetQuarterTurns(selfLocalRotation);

        foreach (var other in _map.GetAnchoredEntities(gridUid, grid, tile))
        {
            if (other == self)
                continue;

            if (!_edgeQuery.TryComp(other, out var edge)
                || edge.LifeStage > ComponentLifeStage.Running
                || edge.ConnectionKey != key
                || TerminatingOrDeleted(other))
                continue;

            var otherXform = Transform(other);
            if (!otherXform.Anchored)
                continue;

            var otherQuarterTurns = GetQuarterTurns(otherXform.LocalRotation);
            if (selfQuarterTurns % 2 != otherQuarterTurns % 2)
                continue;

            var otherAllowed = RotateDirections(edge.AllowedDirections, otherXform.LocalRotation, clockwise: true);
            if ((otherAllowed & requiredDirection) != 0)
                return true;
        }

        return false;
    }

    private void RecalculateNeighbors(TransformComponent xform, EntityUid? except = null)
    {
        if (xform.GridUid is not { } gridUid || !_gridQuery.TryComp(gridUid, out var grid))
            return;

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);

        foreach (var (offset, _, _) in CardinalOffsets)
        {
            foreach (var other in _map.GetAnchoredEntities(gridUid, grid, tile + offset))
            {
                if (other == except)
                    continue;

                if (!_edgeQuery.TryComp(other, out var edgeComp))
                    continue;

                RecalculateEntity((other, edgeComp));
            }
        }
    }

    private bool TryGetGridTile(TransformComponent xform, out EntityUid gridUid, out MapGridComponent grid, out Vector2i tile)
    {
        gridUid = default;
        grid = default!;
        tile = default;

        if (!xform.Anchored || xform.GridUid is not { } gridId || !_gridQuery.TryComp(gridId, out var gridComp))
            return false;

        grid = gridComp;
        gridUid = gridId;
        tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        return true;
    }

    private static EdgeConnectionDirections RotateDirections(EdgeConnectionDirections flags, Angle rotation, bool clockwise)
    {
        var quarterTurns = GetQuarterTurns(rotation);
        if (!clockwise)
            quarterTurns = (4 - quarterTurns) % 4;

        for (var i = 0; i < quarterTurns; i++)
        {
            var rotated = EdgeConnectionDirections.None;
            if ((flags & EdgeConnectionDirections.North) != 0)
                rotated |= EdgeConnectionDirections.East;
            if ((flags & EdgeConnectionDirections.East) != 0)
                rotated |= EdgeConnectionDirections.South;
            if ((flags & EdgeConnectionDirections.South) != 0)
                rotated |= EdgeConnectionDirections.West;
            if ((flags & EdgeConnectionDirections.West) != 0)
                rotated |= EdgeConnectionDirections.North;
            flags = rotated;
        }

        return flags;
    }

    private static int GetQuarterTurns(Angle rotation)
    {
        return ((int) Math.Round(rotation.Degrees / 90.0) % 4 + 4) % 4;
    }

    private static EdgeConnectionDirections FlipEastWest(EdgeConnectionDirections flags)
    {
        var result = flags & ~(EdgeConnectionDirections.East | EdgeConnectionDirections.West);

        if ((flags & EdgeConnectionDirections.East) != 0)
            result |= EdgeConnectionDirections.West;

        if ((flags & EdgeConnectionDirections.West) != 0)
            result |= EdgeConnectionDirections.East;

        return result;
    }
}
