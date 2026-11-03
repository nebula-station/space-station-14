// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Nebula.CCVar;

public sealed partial class NCCVars
{
    /// <summary>
    ///     When disabled, the player can't open the interaction panel and others can't use it on them.
    /// </summary>
    public static readonly CVarDef<bool> ErpPanelEnabled =
        CVarDef.Create("erp.panel_enabled", true, CVar.CLIENT | CVar.ARCHIVE | CVar.REPLICATED);

    /// <summary>
    ///     Percent of arousal the player receives from interactions, applied by the server.
    /// </summary>
    public static readonly CVarDef<float> ErpArousalMultiplier =
        CVarDef.Create("erp.arousal_multiplier", 100f, CVar.CLIENT | CVar.ARCHIVE | CVar.REPLICATED);

    /// <summary>
    ///     Percent of the moan chance of the player, applied by the server.
    /// </summary>
    public static readonly CVarDef<float> ErpMoanMultiplier =
        CVarDef.Create("erp.moan_multiplier", 100f, CVar.CLIENT | CVar.ARCHIVE | CVar.REPLICATED);

    /// <summary>
    ///     Whether interactions play the lean-in animation for this client.
    /// </summary>
    public static readonly CVarDef<bool> ErpLungeEnabled =
        CVarDef.Create("erp.lunge_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Comma separated ids of the favorite interactions.
    /// </summary>
    public static readonly CVarDef<string> ErpFavoriteInteractions =
        CVarDef.Create("erp.favorite_interactions", string.Empty, CVar.CLIENTONLY | CVar.ARCHIVE);
}
