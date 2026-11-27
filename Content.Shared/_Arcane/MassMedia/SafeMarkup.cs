// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/b52ef462f193de8563488db184be5393b9881342/Content.Shared/MassMedia/SafeMarkup.Arcane.cs

using System.Text.RegularExpressions;

namespace Content.Shared._Arcane.MassMedia;

/// <summary>
/// Strips markup tags that players may not use, keeping the ones the client renders for user text (Orion #426).
/// Matches <c>UserFormattableTags.BaseAllowedTags</c> on the client.
/// </summary>
public static class SafeMarkup
{
    private static readonly Regex MarkupTagRegex = new(@"(?<!\\)\[/?(?<tag>[a-zA-Z][a-zA-Z0-9-]*)(?:=[^\]\r\n]*)?/?\]", RegexOptions.Compiled);

    private static readonly HashSet<string> BasicTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "bolditalic",
        "bold",
        "bullet",
        "color",
        "head",
        "italic",
        "mono",
    };

    public static string SanitizeBasic(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return MarkupTagRegex.Replace(text, match => BasicTags.Contains(match.Groups["tag"].Value) ? match.Value : string.Empty);
    }
}
