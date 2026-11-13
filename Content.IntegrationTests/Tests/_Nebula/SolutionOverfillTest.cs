// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;

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

    private const string LegacyManager = "SolutionContainerManager";

    [Test]
    public async Task PrefilledSolutionsFitMaxVolume()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var protoMan = server.ProtoMan;
        var compFactory = server.EntMan.ComponentFactory;
        var serialization = server.ResolveDependency<ISerializationManager>();

        await server.WaitAssertion(() =>
        {
            List<string> errors = [];

            foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (proto.Abstract || proto.ID == WaterFloor)
                    continue;

                if (proto.TryComp<SolutionComponent>(out var solution, compFactory))
                    Check(errors, proto.ID, solution.Id, solution.Solution);

                if (proto.Components.TryGetValue(LegacyManager, out var legacy)
                    && serialization.WriteValue(legacy.Component.GetType(), legacy.Component) is MappingDataNode node
                    && node.TryGet<MappingDataNode>("solutions", out var solutions))
                {
                    foreach (var (name, sol) in solutions)
                    {
                        Check(errors, proto.ID, name, serialization.Read<Solution>(sol));
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
