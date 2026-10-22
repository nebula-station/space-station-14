// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Nebula.InfinityDorms.Components;
using Content.Shared._Nebula.InfinityDorms.Prototypes;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map.Components;

namespace Content.IntegrationTests.Tests._Nebula;

/// <summary>
/// Validates every <see cref="InfinityDormRoomPrototype"/>: references, localization and the dorm grid itself.
/// All problems are collected and reported at once, so one CI run shows every broken dorm.
/// </summary>
[TestFixture]
[TestOf(typeof(InfinityDormRoomPrototype))]
public sealed class InfinityDormMapsTest
{
    private const string RoomNameLoc = "infinity-dorm-room-name-";
    private const string RoomDescLoc = "infinity-dorm-room-desc-";
    private const string CategoryNameLoc = "infinity-dorm-category-name-";

    [Test]
    public async Task RoomPrototypesAreValid()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoMan = server.ProtoMan;
        var resMan = server.ResolveDependency<IResourceManager>();
        var loc = server.ResolveDependency<ILocalizationManager>();

        await server.WaitAssertion(() =>
        {
            var errors = new List<string>();

            var rooms = protoMan.EnumeratePrototypes<InfinityDormRoomPrototype>().OrderBy(r => r.ID).ToList();
            var categories = protoMan.EnumeratePrototypes<InfinityDormRoomCategoryPrototype>().OrderBy(c => c.ID).ToList();

            Assert.That(rooms, Is.Not.Empty, "No infinityDormRoom prototypes found.");
            Assert.That(categories, Is.Not.Empty, "No infinityDormRoomCategory prototypes found.");

            foreach (var category in categories)
            {
                var key = CategoryNameLoc + category.ID.ToLower();
                if (!loc.HasString(key))
                    errors.Add($"Category '{category.ID}': missing localization '{key}'.");

                if (!rooms.Any(r => r.Category.Id == category.ID))
                    errors.Add($"Category '{category.ID}': has no rooms, it would never be shown.");
            }

            foreach (var room in rooms)
            {
                if (!protoMan.HasIndex<InfinityDormRoomCategoryPrototype>(room.Category.Id))
                    errors.Add($"Room '{room.ID}': unknown category '{room.Category.Id}'.");

                if (!resMan.ContentFileExists(room.GridPath))
                    errors.Add($"Room '{room.ID}': grid file '{room.GridPath}' does not exist.");

                foreach (var prefix in new[] { RoomNameLoc, RoomDescLoc })
                {
                    var key = prefix + room.ID.ToLower();
                    if (!loc.HasString(key))
                        errors.Add($"Room '{room.ID}': missing localization '{key}'.");
                }
            }

            Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DormGridsAreValid()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoMan = server.ProtoMan;
        var entMan = server.EntMan;
        var mapSys = entMan.System<SharedMapSystem>();
        var loader = entMan.System<MapLoaderSystem>();

        await server.WaitAssertion(() =>
        {
            var errors = new List<string>();

            foreach (var room in protoMan.EnumeratePrototypes<InfinityDormRoomPrototype>().OrderBy(r => r.ID))
            {
                mapSys.CreateMap(out var mapId);

                try
                {
                    if (!loader.TryLoadGrid(mapId, room.GridPath, out var grid))
                    {
                        errors.Add($"Room '{room.ID}': failed to load grid '{room.GridPath}' (unknown prototype or broken file).");
                        continue;
                    }

                    ValidateGrid(entMan, mapSys, room, grid.Value, errors);
                }
                finally
                {
                    mapSys.DeleteMap(mapId);
                }
            }

            Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        });

        await pair.CleanReturnAsync();
    }

    private static void ValidateGrid(
        IEntityManager entMan,
        SharedMapSystem mapSys,
        InfinityDormRoomPrototype room,
        Entity<MapGridComponent> grid,
        List<string> errors)
    {
        var markers = 0;
        var exits = 0;
        var teleporters = 0;

        var markerQuery = entMan.EntityQueryEnumerator<InfinityDormSpawnMarkerComponent, TransformComponent>();
        while (markerQuery.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid != grid.Owner)
                continue;

            markers++;

            if (!mapSys.TryGetTileRef(grid.Owner, grid.Comp, xform.Coordinates, out var tile) || tile.Tile.IsEmpty)
                errors.Add($"Room '{room.ID}': spawn marker is placed over an empty tile (space).");
        }

        var exitQuery = entMan.EntityQueryEnumerator<InfinityDormExitComponent, TransformComponent>();
        while (exitQuery.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid == grid.Owner)
                exits++;
        }

        var teleporterQuery = entMan.EntityQueryEnumerator<InfinityDormTeleporterComponent, TransformComponent>();
        while (teleporterQuery.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid == grid.Owner)
                teleporters++;
        }

        if (markers != 1)
            errors.Add($"Room '{room.ID}': expected exactly 1 InfinityDormSpawnMarker, found {markers}.");

        if (exits < 1)
            errors.Add($"Room '{room.ID}': no InfinityDormExit found, players would be stuck.");

        if (teleporters > 0)
            errors.Add($"Room '{room.ID}': contains {teleporters} InfinityDormTeleporter(s), this is not allowed.");
    }
}
