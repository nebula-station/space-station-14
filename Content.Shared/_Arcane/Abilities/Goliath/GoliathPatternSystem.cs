// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Shared/Abilities/Goliath/GoliathPatternSystem.cs

using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Map.Components;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Arcane.Abilities.Goliath;

public sealed partial class GoliathPatternSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;

    [SubscribeLocalEvent]
    private void OnSummonTentacles(GoliathSummonTentaclePatternAction args)
    {
        if (args.Handled)
            return;

        _popup.PopupEntity(Loc.GetString("tentacle-ability-use-popup", ("entity", args.Performer)), args.Performer, type: PopupType.SmallCaution);
        _stun.TryAddStunDuration(args.Performer, TimeSpan.FromSeconds(0.8f));

        var half = args.Size / 2;
        List<EntityCoordinates> positions = [];
        for (var x = -half; x <= half; x++)
        {
            for (var y = -half; y <= half; y++)
            {
                var include = args.Pattern switch
                {
                    GoliathTentaclePattern.HollowSquare => Math.Abs(x) == half || Math.Abs(y) == half,
                    GoliathTentaclePattern.DiagonalCross => Math.Abs(x) == Math.Abs(y),
                    _ => true,
                };

                if (include)
                    positions.Add(args.Target.Offset(new Vector2i(x, y)));
            }
        }

        SpawnOnFreeTiles(args.EntityId, positions);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnSummonBabies(GoliathSummonBabyAction args)
    {
        if (args.Handled)
            return;

        List<EntityCoordinates> positions = [];
        positions.Add(args.Target);
        for (var i = 0; i < args.ExtraSpawns; i++)
        {
            positions.Add(args.Target.Offset(_random.NextVector2(args.Offset)));
        }

        SpawnOnFreeTiles(args.EntityId, positions);
        args.Handled = true;
    }

    private void SpawnOnFreeTiles(EntProtoId proto, List<EntityCoordinates> positions)
    {
        if (!_net.IsServer)
            return;

        foreach (var pos in positions)
        {
            if (_transform.GetGrid(pos) is not { } grid
                || !TryComp<MapGridComponent>(grid, out var gridComp)
                || !_map.TryGetTileRef(grid, gridComp, pos, out var tileRef)
                || _turf.IsSpace(tileRef)
                || _turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
            {
                continue;
            }

            SpawnAtPosition(proto, pos);
        }
    }
}
