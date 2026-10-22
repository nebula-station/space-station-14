// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Utility;

namespace Content.Client.Guidebook;

public sealed partial class DocumentParsingManager
{
    /// <summary>
    /// Root of the guide documents that translations mirror.
    /// </summary>
    public static readonly ResPath GuideRoot = new("/ServerInfo");

    /// <summary>
    /// Root of the translated guide documents.
    /// </summary>
    public static readonly ResPath TranslationRoot = new("/ServerInfo/_Nebula/Locale");

    /// <summary>
    /// Returns the translation of a guide document, or the original path if there is none.
    /// </summary>
    /// <param name="path">Path to the original guide document, e.g. <c>/ServerInfo/Guidebook/Engineering/Pumps.xml</c>.</param>
    /// <returns>
    /// <c>/ServerInfo/_Nebula/Locale/Guidebook/Engineering/Pumps.xml</c> if it exists, otherwise <paramref name="path"/>.
    /// </returns>
    public ResPath GetLocalizedGuidePath(ResPath path)
    {
        if (!path.TryRelativeTo(GuideRoot, out var relative))
            return path;

        var localized = TranslationRoot / relative.Value;
        return _resourceManager.ContentFileExists(localized) ? localized : path;
    }

    /// <summary>
    /// Reads a guide document, preferring its translation.
    /// </summary>
    public string ReadGuideText(ResPath path)
    {
        return _resourceManager.ContentFileReadAllText(GetLocalizedGuidePath(path));
    }
}
