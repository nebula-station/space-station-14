// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Nebula.Gravity;
using Content.Shared.Gravity;

namespace Content.IntegrationTests.Tests._Nebula;

/// <summary>
/// The omni-control unit turns gravity on for its whole map and off again once the last one is gone.
/// </summary>
[TestFixture]
[TestOf(typeof(MapGravitySourceSystem))]
public sealed class MapGravitySourceTest
{
    private const string Source = "OmniControlUnit";

    [Test]
    public async Task SourceGivesMapGravity()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var server = pair.Server;
        var entMan = server.EntMan;

        var map = await pair.CreateTestMap();

        EntityUid first = default;
        EntityUid second = default;
        await server.WaitAssertion(() =>
        {
            Assert.That(IsMapGravityOn(entMan, map.MapUid), Is.False, "Test map already has gravity.");

            first = entMan.SpawnEntity(Source, map.GridCoords);
            second = entMan.SpawnEntity(Source, map.GridCoords);
            Assert.That(IsMapGravityOn(entMan, map.MapUid), "Omni-control unit did not give the map gravity.");

            entMan.DeleteEntity(first);
            Assert.That(IsMapGravityOn(entMan, map.MapUid), "Gravity went off while another unit is still on the map.");

            entMan.DeleteEntity(second);
            Assert.That(IsMapGravityOn(entMan, map.MapUid), Is.False, "Gravity stayed on after the last unit was removed.");
        });

        await pair.CleanReturnAsync();
    }

    private static bool IsMapGravityOn(IEntityManager entMan, EntityUid map)
    {
        return entMan.TryGetComponent<GravityComponent>(map, out var gravity) && gravity.Enabled;
    }
}
