// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Storage;
using Content.Trauma.Shared._Nebula.Implants;

namespace Content.IntegrationTests.Tests._Nebula;

/// <summary>
/// Every tool arm spawns with all of its tools inside, and each tool is bound to that arm.
/// </summary>
[TestFixture]
[TestOf(typeof(CyberToolArmSystem))]
public sealed class CyberToolArmTest
{
    [Test]
    public async Task ArmsSpawnWithTheirTools()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var server = pair.Server;
        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;
        var factory = entMan.ComponentFactory;

        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            List<string> errors = [];

            var arms = protoMan.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract && p.HasComp<CyberToolArmComponent>(factory))
                .OrderBy(p => p.ID)
                .ToList();

            Assert.That(arms, Is.Not.Empty, "No tool arm prototypes found.");

            foreach (var proto in arms)
            {
                var arm = entMan.SpawnAtPosition(proto.ID, map.GridCoords);
                var tools = entMan.GetComponent<CyberToolArmComponent>(arm).Tools;
                var stored = entMan.GetComponent<StorageComponent>(arm).Container.ContainedEntities;

                if (stored.Count != tools.Count)
                    errors.Add($"{proto.ID}: {stored.Count} of {tools.Count} tools fit into the storage.");

                foreach (var tool in stored)
                {
                    if (!entMan.TryGetComponent<CyberToolArmItemComponent>(tool, out var item) || item.Arm != arm)
                        errors.Add($"{proto.ID}: {entMan.ToPrettyString(tool)} is not bound to the arm.");
                }

                entMan.DeleteEntity(arm);
            }

            Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        });

        await pair.CleanReturnAsync();
    }
}
