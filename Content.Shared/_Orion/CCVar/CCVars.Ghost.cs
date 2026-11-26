// SPDX-License-Identifier: MIT
// Ported from https://github.com/ArcaneSS14/arcane/blob/045e1f70c57558b76595316d8f399599f85bc753/Content.Shared/_Orion/CCVar/CCVars.Ghost.cs

using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /*
     * Ghost Respawn
     */

    public static readonly CVarDef<float> GhostRespawnTime =
        CVarDef.Create("ghost.respawn_time", 300f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> GhostRespawnMaxPlayers =
        CVarDef.Create("ghost.respawn_max_players", 120, CVar.SERVERONLY);
}
