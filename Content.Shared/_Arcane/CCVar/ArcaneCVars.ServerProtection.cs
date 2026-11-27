// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/b52ef462f193de8563488db184be5393b9881342/Content.Arcane.Common/CCVar/ArcaneCVars.ServerProtection.cs

using Robust.Shared.Configuration;

namespace Content.Shared._Arcane.CCVar;

public sealed partial class ArcaneCVars
{
    /// <summary>
    ///     Punishes players that spam emotes fast enough to lag or crash the server.
    /// </summary>
    public static readonly CVarDef<bool> EmoteProtectionEnabled =
        CVarDef.Create("protection.emote_protection", true, CVar.SERVERONLY);

    /// <summary>
    ///     Ban emote spammers. Takes priority over kicking.
    /// </summary>
    public static readonly CVarDef<bool> EmoteProtectionBanEnabled =
        CVarDef.Create("protection.emote_ban", true, CVar.SERVERONLY);

    /// <summary>
    ///     Ban length for emote spammers in minutes, 0 is permanent.
    /// </summary>
    public static readonly CVarDef<int> EmoteProtectionBanDuration =
        CVarDef.Create("protection.emote_ban_duration", 0, CVar.SERVERONLY);

    /// <summary>
    ///     Kick emote spammers when banning is disabled.
    /// </summary>
    public static readonly CVarDef<bool> EmoteProtectionKickEnabled =
        CVarDef.Create("protection.emote_kick", false, CVar.SERVERONLY);

    /// <summary>
    ///     Erase the character of emote spammers.
    /// </summary>
    public static readonly CVarDef<bool> EmoteProtectionEraseEnabled =
        CVarDef.Create("protection.emote_erase", true, CVar.SERVERONLY);

    /// <summary>
    ///     Delete the chat messages of emote spammers.
    /// </summary>
    public static readonly CVarDef<bool> EmoteProtectionDeleteMessages =
        CVarDef.Create("protection.emote_delete_messages", true, CVar.SERVERONLY);

    /// <summary>
    ///     Emotes within one clear interval that always count as spam.
    /// </summary>
    public static readonly CVarDef<int> EmoteProtectionHardThreshold =
        CVarDef.Create("protection.emote_hard_threshold", 400, CVar.SERVERONLY);

    /// <summary>
    ///     Random reduction of the soft threshold, which is three quarters of the hard one.
    /// </summary>
    public static readonly CVarDef<int> EmoteProtectionSoftVariance =
        CVarDef.Create("protection.emote_soft_variance", 5, CVar.SERVERONLY);

    /// <summary>
    ///     Chance per emote above the soft threshold to count as spam.
    /// </summary>
    public static readonly CVarDef<float> EmoteProtectionPostSoftProbability =
        CVarDef.Create("protection.emote_post_soft_probability", 0.08f, CVar.SERVERONLY);

    /// <summary>
    ///     Seconds between rerolls of the soft threshold.
    /// </summary>
    public static readonly CVarDef<float> EmoteProtectionSoftRefreshCooldown =
        CVarDef.Create("protection.emote_soft_refresh_cooldown", 34f, CVar.SERVERONLY);

    /// <summary>
    ///     Seconds after which emote counts reset.
    /// </summary>
    public static readonly CVarDef<float> EmoteProtectionClearInterval =
        CVarDef.Create("protection.emote_clear_interval", 20f, CVar.SERVERONLY);

    /// <summary>
    ///     Watches admins for mass bans and mass permission removal.
    /// </summary>
    public static readonly CVarDef<bool> AdminActionProtectionEnabled =
        CVarDef.Create("protection.admin_action_enabled", true, CVar.SERVERONLY);

    /// <summary>
    ///     Bans by one admin within the ban window that count as sabotage.
    /// </summary>
    public static readonly CVarDef<int> AdminActionProtectionBanThreshold =
        CVarDef.Create("protection.admin_action_ban_threshold", 5, CVar.SERVERONLY);

    public static readonly CVarDef<float> AdminActionProtectionBanWindowSeconds =
        CVarDef.Create("protection.admin_action_ban_window_seconds", 220f, CVar.SERVERONLY);

    /// <summary>
    ///     Admin removals or restrictions by one admin within the permission window that count as sabotage.
    /// </summary>
    public static readonly CVarDef<int> AdminActionProtectionPermissionThreshold =
        CVarDef.Create("protection.admin_action_permission_threshold", 3, CVar.SERVERONLY);

    public static readonly CVarDef<float> AdminActionProtectionPermissionWindowSeconds =
        CVarDef.Create("protection.admin_action_permission_window_seconds", 120f, CVar.SERVERONLY);

    /// <summary>
    ///     Seconds before the same admin can trigger another alert for the same kind of action.
    /// </summary>
    public static readonly CVarDef<float> AdminActionProtectionAlertCooldownSeconds =
        CVarDef.Create("protection.admin_action_alert_cooldown_seconds", 180f, CVar.SERVERONLY);

    /// <summary>
    ///     De-admin an admin caught sabotaging.
    /// </summary>
    public static readonly CVarDef<bool> AdminActionProtectionAutoDeAdminEnabled =
        CVarDef.Create("protection.admin_action_auto_deadmin", true, CVar.SERVERONLY);

    /// <summary>
    ///     Ban an admin caught sabotaging.
    /// </summary>
    public static readonly CVarDef<bool> AdminActionProtectionAutoBanEnabled =
        CVarDef.Create("protection.admin_action_auto_ban", false, CVar.SERVERONLY);

    /// <summary>
    ///     Ban length for sabotaging admins in minutes, 0 is permanent.
    /// </summary>
    public static readonly CVarDef<int> AdminActionProtectionAutoBanMinutes =
        CVarDef.Create("protection.admin_action_auto_ban_minutes", 0, CVar.SERVERONLY);
}
