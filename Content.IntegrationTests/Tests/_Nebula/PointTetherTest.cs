// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Server._Nebula.PointTether;
using Content.Server._Nebula.Silicons.StationAi;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Nebula;

/// <summary>
/// A tethered AI eye stays put inside the range and returns to the holopad once it leaves it.
/// </summary>
[TestFixture]
[TestOf(typeof(PointTetherSystem))]
public sealed class PointTetherTest
{
    private const float Range = 6f;

    [Test]
    public async Task WalkingEyeReturnsOnlyOutsideRange()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        var entMan = server.EntMan;
        var transform = entMan.System<SharedTransformSystem>();
        var tether = entMan.System<PointTetherSystem>();

        EntityUid anchor = default;
        EntityUid eye = default;

        await server.WaitPost(() =>
        {
            anchor = entMan.SpawnEntity(null, map.GridCoords);
            eye = entMan.SpawnEntity("StationAiHoloWalking", map.GridCoords);

            var walker = entMan.AddComponent<AiHologramWalkerComponent>(eye);
            walker.Holopad = anchor;
            walker.Hologram = EntityUid.Invalid;

            tether.SetTether(eye, anchor, Range, TimeSpan.Zero);
            transform.SetCoordinates(eye, map.GridCoords.Offset(new Vector2(Range - 1f, 0f)));
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var distance = (transform.GetWorldPosition(eye) - transform.GetWorldPosition(anchor)).Length();
            Assert.That(distance, Is.EqualTo(Range - 1f).Within(0.01f));

            transform.SetCoordinates(eye, map.GridCoords.Offset(new Vector2(Range + 1f, 0f)));
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var distance = (transform.GetWorldPosition(eye) - transform.GetWorldPosition(anchor)).Length();
            Assert.That(distance, Is.LessThan(0.01f));
        });

        await pair.CleanReturnAsync();
    }
}
