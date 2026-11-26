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
}
