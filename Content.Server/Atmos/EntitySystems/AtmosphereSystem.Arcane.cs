// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Server/Atmos/EntitySystems/AtmosphereSystem.Hotspot.cs

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    /// <summary>
    /// Fire sprite state (1-7) for a hotspot temperature.
    /// </summary>
    private static byte GetFireState(float temperature)
    {
        return temperature switch
        {
            <= 390.15f => 1,
            <= 475.15f => 2,
            <= 570.15f => 3,
            <= 1000.15f => 4,
            <= 5000.15f => 5,
            <= 10000.15f => 6,
            _ => 7,
        };
    }

    /// <summary>
    /// Hotspot sound keeps the old 1-3 loudness range with the 7 fire states.
    /// </summary>
    private static byte GetFireSoundState(byte state)
    {
        return Math.Min(state, (byte) 3);
    }
}
