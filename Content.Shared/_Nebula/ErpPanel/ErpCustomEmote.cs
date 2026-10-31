// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.ErpPanel;

/// <summary>
/// Player-made emote, stored on their client and sent with every use.
/// </summary>
[Serializable, NetSerializable]
public sealed class ErpCustomEmote
{
    public const int MaxCount = 20;
    public const int MaxNameLength = 32;
    public const int MaxMessageLength = 200;
    public const string TargetPlaceholder = "{target}";
    public const float Range = 1f;

    public string Name = string.Empty;
    public ProtoId<PanelInteractionCategoryPrototype> Category;
    public PanelInteractionKind Kind;
    public ProtoId<PanelCustomSoundPrototype>? Sound;
    public string Message = string.Empty;

    /// <summary>
    /// Used on yourself instead of on a partner.
    /// </summary>
    public bool Self;
    public bool Favorite;

    public bool IsErotic => Kind is PanelInteractionKind.Erotic or PanelInteractionKind.HarmfulErotic;

    /// <summary>
    /// One line without markup, so it is safe both for chat and for the storage file.
    /// </summary>
    public static string Sanitize(string text, int maxLength)
    {
        var builder = new StringBuilder(Math.Min(text.Length, maxLength));
        var space = false;

        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c) || char.IsControl(c))
            {
                space = builder.Length > 0;
                continue;
            }

            if (c is '[' or ']' or '\\')
                continue;

            if (space)
                builder.Append(' ');

            space = false;
            builder.Append(c);

            if (builder.Length >= maxLength)
                break;
        }

        return builder.ToString();
    }
}
