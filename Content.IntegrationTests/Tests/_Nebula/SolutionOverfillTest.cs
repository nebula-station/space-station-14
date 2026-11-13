// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Nebula;

/// <summary>
/// Prefilled solutions fit their max volume, otherwise the excess spills on the floor when the entity spawns.
/// </summary>
[TestFixture]
[TestOf(typeof(SolutionComponent))]
public sealed class SolutionOverfillTest
{
    /// <summary>
    /// Infinite on purpose.
    /// </summary>
    private static readonly EntProtoId WaterFloor = "FloorWaterEntity";

    [Test]
    public async Task PrefilledSolutionsFitMaxVolume()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var protoMan = server.ProtoMan;
        var compFactory = server.EntMan.ComponentFactory;

        await server.WaitAssertion(() =>
        {
            List<string> errors = [];

            foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (proto.Abstract || proto.ID == WaterFloor)
                    continue;

                if (proto.TryComp<SolutionComponent>(out var solution, compFactory))
                    Check(errors, proto.ID, solution.Id, solution.Solution);

#pragma warning disable CS0612
                if (proto.TryComp<SolutionContainerManagerComponent>(out var manager, compFactory) && manager.Solutions != null)
#pragma warning restore CS0612
                {
                    foreach (var (name, sol) in manager.Solutions)
                    {
                        Check(errors, proto.ID, name, sol);
                    }
                }
            }

            Assert.That(errors, Is.Empty, string.Join('\n', errors));
        });

        await pair.CleanReturnAsync();
    }

    private static void Check(List<string> errors, string protoId, string name, Solution solution)
    {
        if (solution.Volume > solution.MaxVolume)
            errors.Add($"{protoId} ({name}): {solution.Volume} > maxVol {solution.MaxVolume}");
    }
}
