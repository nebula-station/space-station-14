// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable

using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Robust.Shared.ContentPack;

namespace Content.IntegrationTests.Tests._Nebula.Localization;

[TestFixture]
public sealed partial class RuLocalizationTest : GameTest
{
    private const string Ru = "ru-RU";
    private const string En = "en-US";
    private const string EntPrefix = "ent-";

    [GeneratedRegex(@"^(-?[a-zA-Z][a-zA-Z0-9_-]*)\s*=(.*)$")]
    private static partial Regex KeyRegex();

    [GeneratedRegex(@"^\s+\.desc\s*=")]
    private static partial Regex DescRegex();

    [GeneratedRegex(@"[a-zA-Z0-9_-]+")]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"^\s*id:\s*(\S+)", RegexOptions.Multiline)]
    private static partial Regex TestProtoIdRegex();

    private static readonly string[] SourcePatterns = ["*.cs", "*.xaml"];
    private static readonly string[] ResourceDirs = ["Prototypes", "ServerInfo"];
    private static readonly string[] ResourcePatterns = ["*.yml", "*.xml"];

    private sealed class FtlEntry(string file)
    {
        public readonly string File = file;
        public bool HasValue;
        public bool HasDesc;
    }

    [Test]
    public void AllEnKeysHaveRuTranslation()
    {
        var res = Server.ResolveDependency<IResourceManager>();
        var en = LoadFtl(res, En, out _);
        var ru = LoadFtl(res, Ru, out _);

        var errors = en
            .Where(kv => !ru.ContainsKey(kv.Key))
            .Select(kv => $"{kv.Value.File}: {kv.Key}")
            .Order()
            .ToList();

        if (errors.Count > 0)
            Assert.Fail($"{errors.Count} en-US keys have no ru-RU translation:\n{string.Join('\n', errors)}");
    }

    [Test]
    public void AllEntitiesHaveRuNames()
    {
        var res = Server.ResolveDependency<IResourceManager>();
        var protoMan = Server.ProtoMan;
        var ru = LoadFtl(res, Ru, out _);
        var testIds = GetTestPrototypeIds();
        var errors = new List<string>();

        foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>().OrderBy(p => p.ID))
        {
            if (testIds.Contains(proto.ID))
                continue;

            bool? nameFromRu = null;
            bool? descFromRu = null;

            foreach (var parent in protoMan.EnumerateParents<EntityPrototype>(proto.ID, true))
            {
                var locId = parent.CustomLocalizationID ?? $"{EntPrefix}{proto.ID}";
                if (ru.TryGetValue(locId, out var entry))
                {
                    if (nameFromRu == null && entry.HasValue)
                        nameFromRu = true;
                    if (descFromRu == null && entry.HasDesc)
                        descFromRu = true;
                }

                if (nameFromRu == null && !string.IsNullOrWhiteSpace(parent.SetName))
                    nameFromRu = false;
                if (descFromRu == null && !string.IsNullOrWhiteSpace(parent.SetDesc))
                    descFromRu = false;

                if (nameFromRu != null && descFromRu != null)
                    break;
            }

            if (nameFromRu == false)
                errors.Add($"{proto.ID}: name \"{proto.Name}\"");
            if (descFromRu == false)
                errors.Add($"{proto.ID}: desc \"{proto.Description}\"");
        }

        if (errors.Count > 0)
            Assert.Fail($"{errors.Count} entity names/descriptions are not translated to ru-RU (add {EntPrefix}<Id>):\n{string.Join('\n', errors)}");
    }

    [Test]
    public void NoExtraRuKeys()
    {
        var res = Server.ResolveDependency<IResourceManager>();
        var protoMan = Server.ProtoMan;
        var en = LoadFtl(res, En, out _);
        var ru = LoadFtl(res, Ru, out var ftlTokens);

        var customLocIds = protoMan.EnumeratePrototypes<EntityPrototype>()
            .SelectMany(p => protoMan.EnumerateParents<EntityPrototype>(p.ID, true))
            .Select(p => p.CustomLocalizationID)
            .OfType<string>()
            .ToHashSet();

        var used = CollectSourceTokens(res);
        used.UnionWith(ftlTokens);

        var errors = new List<string>();
        foreach (var (key, entry) in ru)
        {
            if (en.ContainsKey(key) || customLocIds.Contains(key))
                continue;

            if (key.StartsWith(EntPrefix))
            {
                if (!protoMan.HasMapping<EntityPrototype>(key[EntPrefix.Length..]) && !ftlTokens.Contains(key))
                    errors.Add($"{entry.File}: {key} (no such entity)");
                continue;
            }

            if (!IsUsed(key.TrimStart('-'), used))
                errors.Add($"{entry.File}: {key} (not in en-US and not used anywhere)");
        }

        errors.Sort();
        if (errors.Count > 0)
            Assert.Fail($"{errors.Count} extra ru-RU keys:\n{string.Join('\n', errors)}");
    }

    private static HashSet<string> GetTestPrototypeIds()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;
        var ids = new HashSet<string>();

        foreach (var type in typeof(RuLocalizationTest).Assembly.GetTypes())
        {
            foreach (var field in type.GetFields(flags))
            {
                if (!field.IsDefined(typeof(TestPrototypesAttribute)) || field.GetValue(null) is not string yaml)
                    continue;

                foreach (Match match in TestProtoIdRegex().Matches(yaml))
                {
                    ids.Add(match.Groups[1].Value);
                }
            }
        }

        return ids;
    }

    private static bool IsUsed(string key, HashSet<string> used)
    {
        if (used.Contains(key))
            return true;

        for (var i = key.IndexOf('-'); i >= 0; i = key.IndexOf('-', i + 1))
        {
            if (used.Contains(key[..(i + 1)]))
                return true;
        }

        return false;
    }

    private static Dictionary<string, FtlEntry> LoadFtl(IResourceManager res, string culture, out HashSet<string> valueTokens)
    {
        var entries = new Dictionary<string, FtlEntry>();
        valueTokens = [];

        var files = res.ContentFindFiles(new ResPath($"/Locale/{culture}/"))
            .Where(p => p.Extension == "ftl");

        foreach (var path in files)
        {
            FtlEntry? current = null;
            var inAttribute = false;
            foreach (var line in res.ContentFileReadAllText(path).Split('\n'))
            {
                if (line.StartsWith('#'))
                    continue;

                var match = KeyRegex().Match(line);
                string value;
                if (match.Success)
                {
                    var key = match.Groups[1].Value;
                    current = new FtlEntry(path.ToString());
                    inAttribute = false;
                    entries.TryAdd(key, current);
                    value = match.Groups[2].Value;
                    if (!string.IsNullOrWhiteSpace(value))
                        current.HasValue = true;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(line) || !char.IsWhiteSpace(line[0]))
                        continue;

                    value = line;
                    if (current != null)
                    {
                        if (line.TrimStart().StartsWith('.'))
                        {
                            inAttribute = true;
                            if (DescRegex().IsMatch(line))
                                current.HasDesc = true;
                        }
                        else if (!inAttribute)
                        {
                            current.HasValue = true;
                        }
                    }
                }

                foreach (Match token in TokenRegex().Matches(value))
                {
                    valueTokens.Add(token.Value.TrimStart('-'));
                }
            }
        }

        return entries;
    }

    private static HashSet<string> CollectSourceTokens(IResourceManager res)
    {
        var tokens = new HashSet<string>();
        var root = FindRepoRoot();

        var files = Directory.EnumerateDirectories(root, "Content.*")
            .SelectMany(dir => SourcePatterns.SelectMany(p => Directory.EnumerateFiles(dir, p, SearchOption.AllDirectories)))
            .Select(File.ReadAllText);

        foreach (var text in files)
        {
            AddTokens(tokens, text);
        }

        foreach (var dir in ResourceDirs)
        {
            foreach (var path in res.ContentFindFiles(new ResPath($"/{dir}/")))
            {
                if (ResourcePatterns.Any(p => path.Filename.EndsWith(p[1..])))
                    AddTokens(tokens, res.ContentFileReadAllText(path));
            }
        }

        return tokens;
    }

    private static void AddTokens(HashSet<string> tokens, string text)
    {
        foreach (Match token in TokenRegex().Matches(text))
        {
            tokens.Add(token.Value.TrimStart('-'));
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SpaceStation14.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Could not find SpaceStation14.slnx above the test directory");
    }
}
