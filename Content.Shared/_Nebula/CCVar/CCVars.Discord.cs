// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Nebula.CCVar;

public sealed partial class NCCVars
{
    /// <summary>
    /// Webhook for sending bans to discord
    /// </summary>
    public static readonly CVarDef<string> DiscordBanWebhook =
        CVarDef.Create("discord.ban_webhook", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// Webhook for all logs with PlayTime commands
    /// </summary>
    public static readonly CVarDef<string> DiscordPlayTimeWebhook =
        CVarDef.Create("discord.playtime_webhook", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// Webhook for all logs with permissions panel
    /// </summary>
    public static readonly CVarDef<string> DiscordPermissionsWebhook =
        CVarDef.Create("discord.permissions_webhook", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);
}
