// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Nebula.DetailExaminable;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class DetailExaminableData : IEquatable<DetailExaminableData>
{
    [DataField] public string FlavorText { get; set; } = string.Empty;
    [DataField] public string OOCFlavorText { get; set; } = string.Empty;
    [DataField] public string CharacterFlavorText { get; set; } = string.Empty;
    [DataField] public string GreenFlavorText { get; set; } = string.Empty;
    [DataField] public string YellowFlavorText { get; set; } = string.Empty;
    [DataField] public string RedFlavorText { get; set; } = string.Empty;
    [DataField] public string TagsFlavorText { get; set; } = string.Empty;
    [DataField] public string LinksFlavorText { get; set; } = string.Empty;

    [DataField] public string NSFWFlavorText { get; set; } = string.Empty;
    [DataField] public string NSFWOOCFlavorText { get; set; } = string.Empty;
    [DataField] public string NSFWLinksFlavorText { get; set; } = string.Empty;
    [DataField] public string NSFWTagsFlavorText { get; set; } = string.Empty;
    public DetailExaminableData Sanitized(IConfigurationManager cfg)
    {
        var oocMax = cfg.GetCVar(NCCVars.OOCMaxFlavorTextLength);
        var linksMax = cfg.GetCVar(NCCVars.LinksLength);
        var tagsMax = cfg.GetCVar(NCCVars.TagsLength);

        return new DetailExaminableData
        {
            FlavorText = Sanitize(FlavorText, oocMax),
            OOCFlavorText = Sanitize(OOCFlavorText, oocMax),
            CharacterFlavorText = Sanitize(CharacterFlavorText, cfg.GetCVar(NCCVars.CharacterDescriptionLength)),
            GreenFlavorText = Sanitize(GreenFlavorText, cfg.GetCVar(NCCVars.GreenPreferencesLength)),
            YellowFlavorText = Sanitize(YellowFlavorText, cfg.GetCVar(NCCVars.YellowPreferencesLength)),
            RedFlavorText = Sanitize(RedFlavorText, cfg.GetCVar(NCCVars.RedPreferencesLength)),
            TagsFlavorText = FormatTags(Sanitize(TagsFlavorText, tagsMax)),
            LinksFlavorText = Sanitize(LinksFlavorText, linksMax),

            NSFWFlavorText = Sanitize(NSFWFlavorText, cfg.GetCVar(NCCVars.NSFWPreferencesLength)),
            NSFWOOCFlavorText = Sanitize(NSFWOOCFlavorText, oocMax),
            NSFWLinksFlavorText = Sanitize(NSFWLinksFlavorText, linksMax),
            NSFWTagsFlavorText = FormatTags(Sanitize(NSFWTagsFlavorText, tagsMax)),
        };
    }
    private static string Sanitize(string text, int maxLength)
    {
        var clean = FormattedMessage.RemoveMarkupOrThrow(text);
        return clean.Length > maxLength ? clean[..maxLength] : clean;
    }

    private string FormatTags(string inputTags)
    {
        if (string.IsNullOrWhiteSpace(inputTags))
            return string.Empty;

        var rawTags = inputTags.Split(new[] { ',', ' ', '\n', '\r', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries);
        var formattedTags = new List<string>();

        foreach (var rawTag in rawTags)
        {
            var tag = rawTag.Trim();
            if (string.IsNullOrEmpty(tag))
                continue;

            if (!tag.StartsWith("#"))
            {
                tag = "#" + tag;
            }

            if (tag.Length > 1)
            {
                formattedTags.Add(tag);
            }
        }

        return string.Join(", ", formattedTags);
    }

    public bool Equals(DetailExaminableData? other)
    {
        if (other is null) return false;
        return FlavorText == other.FlavorText
               && OOCFlavorText == other.OOCFlavorText
               && CharacterFlavorText == other.CharacterFlavorText
               && GreenFlavorText == other.GreenFlavorText
               && YellowFlavorText == other.YellowFlavorText
               && RedFlavorText == other.RedFlavorText
               && TagsFlavorText == other.TagsFlavorText
               && LinksFlavorText == other.LinksFlavorText
               && NSFWFlavorText == other.NSFWFlavorText
               && NSFWOOCFlavorText == other.NSFWOOCFlavorText
               && NSFWLinksFlavorText == other.NSFWLinksFlavorText
               && NSFWTagsFlavorText == other.NSFWTagsFlavorText;
    }

    public DetailExaminableData CopyWithChangedField(FlavorTextField field, string text)
    {
        var copy = (DetailExaminableData) MemberwiseClone();

        switch (field)
        {
            case FlavorTextField.Flavor: copy.FlavorText = text; break;
            case FlavorTextField.FlavorOOC: copy.OOCFlavorText = text; break;
            case FlavorTextField.Character: copy.CharacterFlavorText = text; break;
            case FlavorTextField.Green: copy.GreenFlavorText = text; break;
            case FlavorTextField.Yellow: copy.YellowFlavorText = text; break;
            case FlavorTextField.Red: copy.RedFlavorText = text; break;
            case FlavorTextField.Tags: copy.TagsFlavorText = text; break;
            case FlavorTextField.Links: copy.LinksFlavorText = text; break;
            case FlavorTextField.NSFW: copy.NSFWFlavorText = text; break;
            case FlavorTextField.NSFWOOC: copy.NSFWOOCFlavorText = text; break;
            case FlavorTextField.NSFWLinks: copy.NSFWLinksFlavorText = text; break;
            case FlavorTextField.NSFWTags: copy.NSFWTagsFlavorText = text; break;
            default:
                throw new ArgumentOutOfRangeException(nameof(field), field, null);
        }

        return copy;
    }

    public override bool Equals(object? obj) => obj is DetailExaminableData d && Equals(d);
    public override int GetHashCode() => HashCode.Combine(FlavorText, OOCFlavorText, NSFWFlavorText, TagsFlavorText);
}

