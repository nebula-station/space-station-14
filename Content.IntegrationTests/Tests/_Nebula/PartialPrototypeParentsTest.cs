// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Tests._Nebula;

/// <summary>
/// Partial parents get added to the base ones. A parent listed twice freezes prototype loading,
/// so this reads the YAML from disk instead of starting a server.
/// </summary>
[TestFixture]
public sealed class PartialPrototypeParentsTest
{
    /// <summary>
    /// Partial folders in load order, see Content.Trauma.Shared EntryPoint.
    /// </summary>
    private static readonly string[] PartialDirs =
    [
        Path.Combine("_Trauma", "Partials"),
        Path.Combine("_Nebula", "Partials"),
    ];

    [Test]
    public void PartialsHaveNoDuplicateParents()
    {
        var prototypes = FindPrototypesDir();

        Dictionary<(string Kind, string Id), YamlNode> baseParents = [];
        List<(int Order, string Kind, string Id, YamlNode Parents, bool Clear, string File)> partials = [];

        foreach (var file in Directory.EnumerateFiles(prototypes, "*.yml", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(prototypes, file);
            var order = -1;
            for (var i = 0; i < PartialDirs.Length; i++)
            {
                if (relative.StartsWith(PartialDirs[i] + Path.DirectorySeparatorChar))
                    order = i;
            }

            var stream = new YamlStream();
            try
            {
                using var reader = new StreamReader(file);
                stream.Load(reader);
            }
            catch (Exception)
            {
                continue; // the linter catches broken files
            }

            foreach (var document in stream.Documents)
            {
                if (document.RootNode is not YamlSequenceNode root)
                    continue;

                foreach (var mapping in root.OfType<YamlMappingNode>())
                {
                    string? kind = null, id = null;
                    YamlNode? parents = null;
                    var clear = false;
                    foreach (var (key, value) in mapping.Children)
                    {
                        if (key is not YamlScalarNode { Value: { } name })
                            continue;

                        switch (name)
                        {
                            case "type":
                                kind = (value as YamlScalarNode)?.Value;
                                break;
                            case "id":
                                id = (value as YamlScalarNode)?.Value;
                                break;
                            case "parent":
                                parents = value;
                                clear = IsTag(key, "!Clear") || IsTag(value, "!Clear");
                                break;
                        }
                    }

                    if (kind == null || id == null || parents == null)
                        continue;

                    if (order >= 0)
                        partials.Add((order, kind, id, parents, clear, relative));
                    else
                        baseParents[(kind, id)] = parents;
                }
            }
        }

        Dictionary<(string Kind, string Id), List<string>> effective = [];
        foreach (var (key, node) in baseParents)
        {
            effective[key] = Elements(node).Select(e => e.Value ?? string.Empty).ToList();
        }

        List<string> errors = [];
        foreach (var (_, kind, id, node, clear, file) in partials.OrderBy(p => p.Order))
        {
            var key = (kind, id);
            if (!effective.TryGetValue(key, out var parents) || clear
                || node is YamlScalarNode && baseParents.GetValueOrDefault(key) is YamlScalarNode or null)
            {
                parents = [];
            }

            foreach (var parent in Elements(node))
            {
                var value = parent.Value ?? string.Empty;
                var tag = parent.Tag.IsEmpty ? null : parent.Tag.Value;
                if (tag == "!Remove")
                    parents.Remove(value);
                else if (tag?.StartsWith("!Index:") == true && int.TryParse(tag["!Index:".Length..], out var index))
                    parents.Insert(Math.Clamp(index < 0 ? parents.Count + index : index, 0, parents.Count), value);
                else
                    parents.Add(value);
            }

            effective[key] = parents;

            var duplicates = parents.GroupBy(p => p).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicates.Count > 0)
            {
                errors.Add($"{file}: {kind} {id} ends up with parents [{string.Join(", ", parents)}], "
                    + $"{string.Join(", ", duplicates)} is listed twice and freezes prototype loading. "
                    + "Only list the new parent in the partial.");
            }
        }

        Assert.That(errors, Is.Empty, string.Join("\n", errors));
    }

    private static bool IsTag(YamlNode node, string tag)
    {
        return !node.Tag.IsEmpty && node.Tag.Value == tag;
    }

    private static IEnumerable<YamlScalarNode> Elements(YamlNode node)
    {
        return node switch
        {
            YamlScalarNode scalar => [scalar],
            YamlSequenceNode seq => seq.OfType<YamlScalarNode>(),
            _ => [],
        };
    }

    private static string FindPrototypesDir()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Resources", "Prototypes");
            if (Directory.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        Assert.Fail("Could not find Resources/Prototypes above the test directory.");
        return string.Empty;
    }
}
