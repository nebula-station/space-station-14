// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.IO;
using System.Linq;
using Content.Shared.Decals;
using Robust.Shared.ContentPack;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Tests._Nebula.Maps;

[TestFixture]
[TestOf(typeof(DecalPrototype))]
public sealed class DecalMapsTest
{
    [Test]
    public async Task AllMapDecalsExist()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var resMan = server.ResolveDependency<IResourceManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        var errors = new List<string>();

        await server.WaitAssertion(() =>
        {
            foreach (var path in resMan.ContentFindFiles(new ResPath("/Maps")))
            {
                if (path.Extension != "yml")
                    continue;

                string text;
                using (var reader = new StreamReader(resMan.ContentFileRead(path)))
                {
                    text = reader.ReadToEnd();
                }

                if (!text.Contains("DecalGrid"))
                    continue;

                var stream = new YamlStream();
                stream.Load(new StringReader(text));

                foreach (var document in stream.Documents)
                {
                    foreach (var id in FindDecalIds(document.RootNode).Distinct())
                    {
                        if (!protoMan.HasIndex<DecalPrototype>(id))
                            errors.Add($"{path}: Unknown decal '{id}'");
                    }
                }
            }
        });

        Assert.That(errors, Is.Empty, string.Join("\n", errors));
        await pair.CleanReturnAsync();
    }

    private static IEnumerable<string> FindDecalIds(YamlNode node)
    {
        switch (node)
        {
            case YamlMappingNode map:
                foreach (var (key, value) in map.Children)
                {
                    if (key is YamlScalarNode { Value: "node" } &&
                        value is YamlMappingNode nodeMap &&
                        nodeMap.Children.TryGetValue(new YamlScalarNode("id"), out var idNode) &&
                        idNode is YamlScalarNode { Value: { } id })
                    {
                        yield return id;
                    }
                    else
                    {
                        foreach (var inner in FindDecalIds(value))
                            yield return inner;
                    }
                }
                break;

            case YamlSequenceNode sequence:
                foreach (var child in sequence.Children)
                {
                    foreach (var inner in FindDecalIds(child))
                        yield return inner;
                }
                break;
        }
    }
}
