// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Server/StationEvents/Components/RandomSpawnRuleComponent.cs

namespace Content.Server.StationEvents.Components;

public sealed partial class RandomSpawnRuleComponent
{
    /// <summary>
    /// Minimum number of entities to spawn.
    /// </summary>
    [DataField]
    public int MinCount = 1;

    /// <summary>
    /// Maximum number of entities to spawn.
    /// </summary>
    [DataField]
    public int MaxCount = 1;
}
