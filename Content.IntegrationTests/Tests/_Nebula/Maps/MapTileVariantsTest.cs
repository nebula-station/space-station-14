// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.IO;
using System.Linq;
using Content.Shared.Maps;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Tests._Nebula.Maps;

/// <summary>
/// A saved tile variant at or above the prototype's <see cref="ContentTileDefinition.Variants"/> is drawn as the
/// missing tile texture. Happens when a tile texture is swapped for one with fewer variants.
/// Fix with the <c>variantize</c> command on the grid and resave.
/// </summary>
[TestFixture]
[TestOf(typeof(ContentTileDefinition))]
public sealed class MapTileVariantsTest
{
    private static readonly ResPath MapsPath = new("/Maps/_Nebula");

    [Test]
    public async Task NebulaMapTileVariantsInRange()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var resMan = server.ResolveDependency<IResourceManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var tileDefMan = server.ResolveDependency<ITileDefinitionManager>();

        List<string> errors = [];

        await server.WaitAssertion(() =>
        {
            var aliases = protoMan.EnumeratePrototypes<TileAliasPrototype>().ToDictionary(p => p.ID, p => p.Target);

            foreach (var path in resMan.ContentFindFiles(MapsPath))
            {
                if (path.Extension != "yml")
                    continue;

                var stream = new YamlStream();
                using (var reader = new StreamReader(resMan.ContentFileRead(path)))
                {
                    stream.Load(reader);
                }

                foreach (var document in stream.Documents)
                {
                    if (document.RootNode is not YamlMappingNode root
                        || !root.Children.TryGetValue(new YamlScalarNode("tilemap"), out var tileMapNode)
                        || tileMapNode is not YamlMappingNode tileMap)
                        continue;

                    Dictionary<int, (string Name, byte Variants)> variants = [];
                    foreach (var (key, value) in tileMap.Children)
                    {
                        var name = ((YamlScalarNode) value).Value!;
                        if (aliases.TryGetValue(name, out var target))
                            name = target;

                        if (tileDefMan.TryGetDefinition(name, out var def) && def is ContentTileDefinition content)
                            variants[int.Parse(((YamlScalarNode) key).Value!)] = (name, content.Variants);
                    }

                    Dictionary<string, int> bad = [];
                    foreach (var chunk in FindChunks(root))
                    {
                        CheckChunk(chunk, variants, bad);
                    }

                    foreach (var (name, count) in bad)
                    {
                        errors.Add($"{path}: {name} has {count} tiles with a variant out of range");
                    }
                }
            }
        });

        Assert.That(errors, Is.Empty, string.Join("\n", errors));
        await pair.CleanReturnAsync();
    }

    private static void CheckChunk(YamlMappingNode chunk, Dictionary<int, (string Name, byte Variants)> variants,
        Dictionary<string, int> bad)
    {
        var bytes = Convert.FromBase64String(((YamlScalarNode) chunk[new YamlScalarNode("tiles")]).Value!);
        var version = chunk.Children.TryGetValue(new YamlScalarNode("version"), out var versionNode)
            ? int.Parse(((YamlScalarNode) versionNode).Value!)
            : 1;

        var step = version >= 7 ? 7 : version >= 6 ? 6 : 4;
        var variantOffset = version >= 6 ? 5 : 3;

        for (var offset = 0; offset + step <= bytes.Length; offset += step)
        {
            var id = version >= 6 ? BitConverter.ToInt32(bytes, offset) : BitConverter.ToUInt16(bytes, offset);
            if (!variants.TryGetValue(id, out var def) || bytes[offset + variantOffset] < def.Variants)
                continue;

            bad[def.Name] = bad.GetValueOrDefault(def.Name) + 1;
        }
    }

    private static IEnumerable<YamlMappingNode> FindChunks(YamlNode node)
    {
        switch (node)
        {
            case YamlMappingNode map:
                if (map.Children.ContainsKey(new YamlScalarNode("tiles"))
                    && map.Children.ContainsKey(new YamlScalarNode("ind")))
                {
                    yield return map;
                    yield break;
                }

                foreach (var value in map.Children.Values)
                {
                    foreach (var inner in FindChunks(value))
                        yield return inner;
                }
                break;

            case YamlSequenceNode sequence:
                foreach (var child in sequence.Children)
                {
                    foreach (var inner in FindChunks(child))
                        yield return inner;
                }
                break;
        }
    }
}
