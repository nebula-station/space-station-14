// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Nebula.CCVar;

public sealed partial class NCCVars
{

    /// <summary>
    ///      Sets the maximum Custom Species length.
    /// </summary>
    public static readonly CVarDef<int> MaxCustomSpeciesLength =
        CVarDef.Create("ic.custom_species_length", 16, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Sets the maximum length for OOC flavor text.
    /// </summary>
    public static readonly CVarDef<int> OOCMaxFlavorTextLength =
        CVarDef.Create("ic.oocflavor_text_length", 2500, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Sets the maximum length for character description text.
    /// </summary>
    public static readonly CVarDef<int> CharacterDescriptionLength =
        CVarDef.Create("ic.character_description_length", 5000, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Sets the maximum length for green preferences text.
    /// </summary>
    public static readonly CVarDef<int> GreenPreferencesLength =
        CVarDef.Create("ic.green_preferences_length", 2500, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Sets the maximum length for yellow preferences text.
    /// </summary>
    public static readonly CVarDef<int> YellowPreferencesLength =
        CVarDef.Create("ic.yellow_preferences_length", 2500, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Sets the maximum length for red preferences text.
    /// </summary>
    public static readonly CVarDef<int> RedPreferencesLength =
        CVarDef.Create("ic.red_preferences_length", 2500, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Sets the maximum length for tags text.
    /// </summary>
    public static readonly CVarDef<int> TagsLength =
        CVarDef.Create("ic.tags_length", 256, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Sets the maximum length for links text.
    /// </summary>
    public static readonly CVarDef<int> LinksLength =
        CVarDef.Create("ic.links_length", 512, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Sets the maximum length for NSFW preferences text.
    /// </summary>
    public static readonly CVarDef<int> NSFWPreferencesLength =
        CVarDef.Create("ic.nsfw_preferences_length", 2500, CVar.SERVER | CVar.REPLICATED);
}
