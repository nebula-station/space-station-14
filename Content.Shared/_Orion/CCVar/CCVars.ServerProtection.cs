// SPDX-License-Identifier: MIT
// Ported from https://github.com/dead-space-server/EridaGoob/blob/312c4de3ed33142c2a488771f4d1f58bbc499974/Content.Shared/_Orion/CCVar/CCVars.ServerProtection.cs

using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Protect IC and OOC chat from banned words.
    /// </summary>
    public static readonly CVarDef<bool> ChatProtectionEnabled =
        CVarDef.Create("protection.chat_protection", true, CVar.SERVERONLY);

    /// <summary>
    /// Ban the player when violating chat rules.
    /// </summary>
    public static readonly CVarDef<bool> ChatProtectionBanEnabled =
        CVarDef.Create("protection.chat_ban", false, CVar.SERVERONLY);

    /// <summary>
    /// Kick the player (if ban is disabled) when violating chat rules.
    /// </summary>
    public static readonly CVarDef<bool> ChatProtectionKickEnabled =
        CVarDef.Create("protection.chat_kick", true, CVar.SERVERONLY);

    /// <summary>
    /// Erase the character when violating IC chat rules.
    /// </summary>
    public static readonly CVarDef<bool> ChatProtectionEraseEnabled =
        CVarDef.Create("protection.chat_erase", false, CVar.SERVERONLY);

    /// <summary>
    /// Delete all messages of the player when violating chat rules.
    /// </summary>
    public static readonly CVarDef<bool> ChatProtectionDeleteMessages =
        CVarDef.Create("protection.chat_delete_messages", false, CVar.SERVERONLY);

    /// <summary>
    /// Ban duration in minutes for chat violations. 0 means permanent.
    /// </summary>
    public static readonly CVarDef<int> ChatProtectionBanDuration =
        CVarDef.Create("protection.chat_ban_duration", 0, CVar.SERVERONLY);
}
