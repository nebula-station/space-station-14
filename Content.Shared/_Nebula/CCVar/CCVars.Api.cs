// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Nebula.CCVar;

public sealed partial class NCCVars
{
    public static readonly CVarDef<string> ApiToken =
        CVarDef.Create("napi.key", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> ApiUrl =
        CVarDef.Create("napi.url", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// Seconds between attempts to reconnect to the API while it is inactive.
    /// </summary>
    public static readonly CVarDef<float> ApiHealthCheckInterval =
        CVarDef.Create("napi.health_check_interval", 30f, CVar.SERVERONLY);
}
