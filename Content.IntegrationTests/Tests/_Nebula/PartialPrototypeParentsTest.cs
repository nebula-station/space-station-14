// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Nebula;

/// <summary>
/// A partial that drops a parent declared by the base file leaves a stale edge in the engine's inheritance graph
/// (MultiRootInheritanceGraph.Add never removes it). The stale parent then releases the child before its real parent
/// is resolved, so the prototype randomly loses inherited components, differently on client and server.
/// The race itself can't be tested reliably, so this checks the YAML that causes it.
/// </summary>
[TestFixture]
public sealed class PartialPrototypeParentsTest : GameTest
{
    private static readonly ResPath PrototypesDir = new("/Prototypes");
    private static readonly ResPath PartialsDir = new("/Prototypes/_Trauma/Partials");

    /// <summary>
    /// Prototypes that still have the race: these partials replace the parent outright, so the fix is changing
    /// <c>parent</c> in the base file. Remove entries as they get fixed.
    /// </summary>
    private static readonly HashSet<string> KnownViolations =
    [
        "BaseChangelingRule",
        "BaseHandheldCrewMonitor",
        "BaseWeaponPowerCellSmall",
        "DefaultStationBeaconRobotics",
        "EncryptionKeyRobo",
        "Stunprod",
        "WardrobeChapel",
        "WeaponEnergyCrossbowBase",
    ];

    [Test]
    public async Task PartialsKeepBaseParents()
    {
        var resources = Server.ResolveDependency<IResourceManager>();

        Dictionary<(string Kind, string Id), List<string>> baseParents = [];
        List<(string Kind, string Id, List<string> Parents, ResPath File)> partials = [];

        foreach (var file in resources.ContentFindFiles(PrototypesDir).Where(f => f.Extension == "yml"))
        {
            var isPartial = file.TryRelativeTo(PartialsDir, out _);
            using var reader = resources.ContentFileReadText(file);

            foreach (var document in DataNodeParser.ParseYamlStream(reader))
            {
                if (document.Root is not SequenceDataNode prototypes)
                    continue;

                foreach (var mapping in prototypes.OfType<MappingDataNode>())
                {
                    if (!mapping.TryGet<ValueDataNode>("type", out var kind)
                        || !mapping.TryGet<ValueDataNode>("id", out var id)
                        || !TryGetParents(mapping, out var parents))
                        continue;

                    if (isPartial)
                        partials.Add((kind.Value, id.Value, parents, file));
                    else
                        baseParents[(kind.Value, id.Value)] = parents;
                }
            }
        }

        List<string> errors = [];
        HashSet<string> violating = [];

        foreach (var (kind, id, parents, file) in partials)
        {
            if (!baseParents.TryGetValue((kind, id), out var original))
                continue;

            var dropped = original.Except(parents).ToList();
            if (dropped.Count == 0)
                continue;

            violating.Add(id);
            if (KnownViolations.Contains(id))
                continue;

            errors.Add($"{file}: {kind} {id} drops base parent(s) {string.Join(", ", dropped)}, which makes prototype loading race. "
                + "If the dropped parent is an ancestor of the new one, list it last: parent: [ New, Old ]. "
                + "Otherwise change parent in the base file instead of the partial.");
        }

        foreach (var id in KnownViolations.Except(violating))
        {
            errors.Add($"{id} no longer drops a base parent, remove it from {nameof(KnownViolations)}.");
        }

        Assert.That(errors, Is.Empty, string.Join("\n", errors));
    }

    private static bool TryGetParents(MappingDataNode mapping, out List<string> parents)
    {
        parents = [];
        if (mapping.TryGet<ValueDataNode>("parent", out var single))
            parents.Add(single.Value);
        else if (mapping.TryGet<SequenceDataNode>("parent", out var list))
            parents.AddRange(list.OfType<ValueDataNode>().Select(parent => parent.Value));

        return parents.Count > 0;
    }
}
