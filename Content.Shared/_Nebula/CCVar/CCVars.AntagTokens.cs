// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Nebula.CCVar;

public sealed partial class NCCVars
{
    /// <summary>
    /// Identifies this game server in the bot. Purchases are closed while it is empty.
    /// Every server that uses the same bot needs its own value.
    /// </summary>
    public static readonly CVarDef<string> AntagTokensServerId =
        CVarDef.Create("nebula.antag_tokens.server_id", string.Empty, CVar.SERVERONLY);

    /// <summary>
    /// Earliest moment of the antag grant, in minutes from the round start.
    /// </summary>
    public static readonly CVarDef<float> AntagTokensWindowMin =
        CVarDef.Create("nebula.antag_tokens.window_min", 5f, CVar.SERVERONLY);

    /// <summary>
    /// Latest moment of the antag grant, in minutes from the round start. Purchases still not granted are refunded.
    /// </summary>
    public static readonly CVarDef<float> AntagTokensWindowMax =
        CVarDef.Create("nebula.antag_tokens.window_max", 15f, CVar.SERVERONLY);

    /// <summary>
    /// Seconds between checks of purchases that could not be granted yet and of failed bot calls.
    /// </summary>
    public static readonly CVarDef<float> AntagTokensRetryInterval =
        CVarDef.Create("nebula.antag_tokens.retry_interval", 30f, CVar.SERVERONLY);

    /// <summary>
    /// Comma-separated game presets where a purchase is granted. In any other preset it is refunded.
    /// </summary>
    public static readonly CVarDef<string> AntagTokensPresets =
        CVarDef.Create("nebula.antag_tokens.presets", "SecretPlusLow,SecretPlusMid,SecretPlusHigh,Dynamic", CVar.SERVERONLY);
}
