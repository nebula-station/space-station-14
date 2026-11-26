// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Server/StationGoal/StationGoalReceiverComponent.cs

namespace Content.Server._Arcane.StationGoal;

/// <summary>
///     Fax machine that prints station goals.
/// </summary>
[RegisterComponent, Access(typeof(StationGoalSystem))]
public sealed partial class StationGoalReceiverComponent : Component
{
    /// <summary>
    ///     Receive the goals of every station, not only of the station this fax is on.
    /// </summary>
    [DataField]
    public bool AllStations;
}
