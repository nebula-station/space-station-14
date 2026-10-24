// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Nebula.CCVar;

public sealed partial class NCCVars
{
    /// <summary>
    ///     Hex seed color the Nebula UI palette is generated from.
    /// </summary>
    public static readonly CVarDef<string> InterfaceAccent =
        CVarDef.Create(name: "nebula.ui.accent", "#303AA8", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Whether Nebula UI panels let the blurred background show through.
    /// </summary>
    public static readonly CVarDef<bool> InterfaceTransparency =
        CVarDef.Create("nebula.ui.transparency", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     How transparent Nebula UI panels are when <see cref="InterfaceTransparency"/> is on.
    /// </summary>
    public static readonly CVarDef<float> InterfaceTransparencyAmount =
        CVarDef.Create("nebula.ui.transparency_amount", 0.11f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Whether the lobby shows the chat panel on the left and the character panel on the right.
    /// </summary>
    public static readonly CVarDef<bool> LobbyChatLeft =
        CVarDef.Create("nebula.ui.lobby_chat_left", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Whether the lobby shows the background picked by the server for the round.
    /// </summary>
    public static readonly CVarDef<bool> LobbyBackgroundRandom =
        CVarDef.Create("nebula.ui.lobby_background_random", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Lobby background prototype shown while <see cref="LobbyBackgroundRandom"/> is off.
    /// </summary>
    public static readonly CVarDef<string> LobbyBackground =
        CVarDef.Create("nebula.ui.lobby_background", "", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Whether lobby players can open the list of jobs the ready players want.
    /// </summary>
    public static readonly CVarDef<bool> LobbyReadyManifest =
        CVarDef.Create("nebula.lobby.ready_manifest", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Link to the server's Boosty page, the lobby support button is hidden while empty.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksBoosty =
        CVarDef.Create("infolinks.boosty", "https://boosty.to/ss14.nebula", CVar.SERVER | CVar.REPLICATED);
}
