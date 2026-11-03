// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.ErpPanel;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.ErpPanel;

/// <summary>
/// Custom emotes live in the client user data, one tab-separated line each.
/// </summary>
public static class ErpCustomEmoteStorage
{
    private static readonly ResPath FilePath = new("/erp_custom_emotes.txt");
    private const int FieldCount = 7;

    public static List<ErpCustomEmote> Load(IResourceManager resources, IPrototypeManager prototypes)
    {
        var emotes = new List<ErpCustomEmote>();

        try
        {
            if (!resources.UserData.Exists(FilePath))
                return emotes;

            using var reader = resources.UserData.OpenText(FilePath);
            while (reader.ReadLine() is { } line && emotes.Count < ErpCustomEmote.MaxCount)
            {
                var fields = line.Split('\t');
                if (fields.Length != FieldCount)
                    continue;

                if (!prototypes.HasIndex<PanelInteractionCategoryPrototype>(fields[1]) ||
                    !Enum.TryParse<PanelInteractionKind>(fields[2], out var kind) ||
                    !Enum.IsDefined(kind))
                    continue;

                ProtoId<PanelCustomSoundPrototype>? sound = null;
                if (prototypes.HasIndex<PanelCustomSoundPrototype>(fields[3]))
                    sound = fields[3];

                var emote = new ErpCustomEmote
                {
                    Name = ErpCustomEmote.Sanitize(fields[0], ErpCustomEmote.MaxNameLength),
                    Category = fields[1],
                    Kind = kind,
                    Sound = sound,
                    Message = ErpCustomEmote.Sanitize(fields[4], ErpCustomEmote.MaxMessageLength),
                    Self = fields[5] == "1",
                    Favorite = fields[6] == "1",
                };

                if (emote.Name.Length > 0 && emote.Message.Length > 0)
                    emotes.Add(emote);
            }
        }
        catch (Exception e)
        {
            Logger.GetSawmill("erp").Warning($"Failed to load custom emotes: {e.Message}");
        }

        return emotes;
    }

    public static void Save(IResourceManager resources, List<ErpCustomEmote> emotes)
    {
        try
        {
            using var writer = resources.UserData.OpenWriteText(FilePath);
            foreach (var emote in emotes)
            {
                // Sanitize already dropped tabs and newlines
                // explicit array, the params span overload is rejected by the client sandbox
                writer.WriteLine(string.Join('\t', new[]
                {
                    emote.Name,
                    emote.Category.Id,
                    emote.Kind.ToString(),
                    emote.Sound?.Id ?? string.Empty,
                    emote.Message,
                    emote.Self ? "1" : "0",
                    emote.Favorite ? "1" : "0",
                }));
            }
        }
        catch (Exception e)
        {
            Logger.GetSawmill("erp").Warning($"Failed to save custom emotes: {e.Message}");
        }
    }
}
