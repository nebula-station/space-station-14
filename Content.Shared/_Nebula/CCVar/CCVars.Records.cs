// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Nebula.CCVar;

public sealed partial class NCCVars
{
    // bans and notes stay on this server. off = shared
    public static readonly CVarDef<bool> IsolateRecords =
        CVarDef.Create("nebula.isolate_records", false, CVar.SERVERONLY);
}
