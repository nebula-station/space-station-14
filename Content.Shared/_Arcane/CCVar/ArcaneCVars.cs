// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Common/CCVar/ArcaneCVars.cs

using Robust.Shared.Configuration;

namespace Content.Shared._Arcane.CCVar;

[CVarDefs]
public sealed partial class ArcaneCVars
{
    /// <summary>
    ///     Whether stations are sent a random station goal by fax when the round starts.
    /// </summary>
    public static readonly CVarDef<bool> StationGoal =
        CVarDef.Create("game.station_goal", true, CVar.SERVERONLY);

    /// <summary>
    ///     Volume of the radio bark played when a headset receives a message.
    /// </summary>
    public static readonly CVarDef<float> RadioBarkVolume =
        CVarDef.Create("audio.radio_bark_volume", 0.5f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Minutes between two paydays.
    /// </summary>
    public static readonly CVarDef<float> PaydayInterval =
        CVarDef.Create("economy.payday_interval", 5f, CVar.SERVERONLY);

    /// <summary>
    ///     Personal cargo orders cost this much more, the difference goes to the ordering account.
    /// </summary>
    public static readonly CVarDef<float> PrivateOrderMarkup =
        CVarDef.Create("economy.private_order_markup", 1.1f, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Enables directional emotes, seen only by the sender and the target.
    /// </summary>
    public static readonly CVarDef<bool> DirectionalEmotesEnabled =
        CVarDef.Create("chat.directional_emotes_enabled", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Maximum distance to the target of a directional emote.
    /// </summary>
    public static readonly CVarDef<int> DirectionalEmoteRange =
        CVarDef.Create("chat.directional_emotes_range", 3, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Maximum length of a directional emote.
    /// </summary>
    public static readonly CVarDef<int> DirectionalEmoteMaxLength =
        CVarDef.Create("chat.directional_emotes_max_length", 10000, CVar.SERVER | CVar.REPLICATED);
}
