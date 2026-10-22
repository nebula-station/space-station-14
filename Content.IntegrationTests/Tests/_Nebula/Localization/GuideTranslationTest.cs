// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable

using System.Linq;
using Content.Client.Guidebook;
using Content.Client.Guidebook.Richtext;
using Content.Shared.Guidebook;
using Robust.Shared.ContentPack;

namespace Content.IntegrationTests.Tests._Nebula.Localization;

[TestFixture]
[TestOf(typeof(DocumentParsingManager))]
public sealed class GuideTranslationTest : GameTest
{
    /// <summary>
    /// Every translated guide must mirror an existing original that a guidebook entry uses.
    /// </summary>
    [Test]
    public void AllTranslationsHaveOriginal()
    {
        var res = Client.ResolveDependency<IResourceManager>();
        var guides = CProtoMan.EnumeratePrototypes<GuideEntryPrototype>()
            .Select(p => p.Text)
            .ToHashSet();
        var errors = new List<string>();

        foreach (var path in FindTranslations(res))
        {
            var original = DocumentParsingManager.GuideRoot / path.RelativeTo(DocumentParsingManager.TranslationRoot);
            if (!res.ContentFileExists(original))
            {
                errors.Add($"{path}: original {original} does not exist");
                continue;
            }

            if (!guides.Contains(original))
                errors.Add($"{path}: no guide entry uses {original}");
        }

        if (errors.Count > 0)
            Assert.Fail($"{errors.Count} invalid guide translations:\n{string.Join('\n', errors)}");
    }

    /// <summary>
    /// Every translated guide must parse into guidebook controls.
    /// </summary>
    [Test]
    public async Task AllTranslationsParse()
    {
        var res = Client.ResolveDependency<IResourceManager>();
        var parser = Client.ResolveDependency<DocumentParsingManager>();
        var errors = new List<string>();

        await Client.WaitAssertion(() =>
        {
            foreach (var path in FindTranslations(res))
            {
                if (!parser.TryAddMarkup(new Document(), res.ContentFileReadAllText(path)))
                    errors.Add(path.ToString());
            }
        });

        if (errors.Count > 0)
            Assert.Fail($"{errors.Count} guide translations failed to parse:\n{string.Join('\n', errors)}");
    }

    private static List<ResPath> FindTranslations(IResourceManager res)
    {
        return res.ContentFindFiles(DocumentParsingManager.TranslationRoot)
            .Where(p => p.Extension == "xml")
            .OrderBy(p => p.ToString())
            .ToList();
    }
}
