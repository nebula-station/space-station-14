// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Shared/StationGoal/StationGoalPrototype.cs

using Robust.Shared.Prototypes;

namespace Content.Shared._Arcane.StationGoal;

/// <summary>
///     A secondary objective sent to the station by fax at round start.
/// </summary>
[Prototype]
public sealed partial class StationGoalPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Text of the fax, gets the station name as <c>station</c>.
    /// </summary>
    [DataField(required: true)]
    public LocId Text;

    [DataField]
    public int? MinPlayers;

    [DataField]
    public int? MaxPlayers;

    /// <summary>
    ///     Items the goal needs, spawned next to every fax that receives it.
    /// </summary>
    [DataField]
    public List<EntProtoId> Spawns = [];
}
