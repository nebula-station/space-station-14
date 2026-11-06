// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Nebula.CCVar;

public sealed partial class NCCVars
{
    public static readonly CVarDef<string> DiscordBanWebhook =
        CVarDef.Create("discord.ban_webhook", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> DiscordPlayTimeWebhook =
        CVarDef.Create("discord.playtime_webhook", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> DiscordTokensWebhook =
        CVarDef.Create("discord.tokens_webhook", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> DiscordPermissionsWebhook =
        CVarDef.Create("discord.permissions_webhook", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> DiscordAccountLinkingMessageLink =
        CVarDef.Create("discord.discord_account_linking_message_link", "", CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    /// Refuse connections from players without a linked Discord account.
    /// </summary>
    public static readonly CVarDef<bool> DiscordRequireLink =
        CVarDef.Create("discord.require_link", false, CVar.SERVERONLY);
}
