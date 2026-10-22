// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Nebula.CCVar;

public sealed partial class NCCVars
{
    /// <summary>
    /// In minutes
    /// </summary>
    public static readonly CVarDef<int> RoomDeleteTimer =
        CVarDef.Create("dorms.room_delete_timer", 30, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> RoomsLimit =
        CVarDef.Create("dorms.rooms_limit", 4, CVar.SERVER | CVar.REPLICATED);

}
