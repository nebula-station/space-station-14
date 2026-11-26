// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Shared/Abilities/Goliath/GoliathPatternActions.cs

using Content.Shared.Actions;

namespace Content.Shared._Arcane.Abilities.Goliath;

/// <summary>
/// Shape of the tentacle area summoned by <see cref="GoliathSummonTentaclePatternAction"/>.
/// </summary>
public enum GoliathTentaclePattern : byte
{
    /// <summary>
    /// Every tile of the square.
    /// </summary>
    Square,

    /// <summary>
    /// Only the border of the square.
    /// </summary>
    HollowSquare,

    /// <summary>
    /// Both diagonals of the square.
    /// </summary>
    DiagonalCross,
}

/// <summary>
/// Summons goliath tentacles in a square-based pattern around the target.
/// </summary>
public sealed partial class GoliathSummonTentaclePatternAction : WorldTargetActionEvent
{
    [DataField]
    public EntProtoId EntityId = "EffectGoliathTentacleSpawn";

    [DataField]
    public GoliathTentaclePattern Pattern = GoliathTentaclePattern.Square;

    /// <summary>
    /// Side length of the square, in tiles. Odd values center on the target.
    /// </summary>
    [DataField]
    public int Size = 3;
}

/// <summary>
/// Summons goliath babies at the target and around it.
/// </summary>
public sealed partial class GoliathSummonBabyAction : WorldTargetActionEvent
{
    [DataField]
    public EntProtoId EntityId = "MobGoliathBaby";

    /// <summary>
    /// How many babies spawn in addition to the one at the target.
    /// </summary>
    [DataField]
    public int ExtraSpawns = 1;

    /// <summary>
    /// Maximum distance of the extra spawns from the target.
    /// </summary>
    [DataField]
    public float Offset = 4f;
}
