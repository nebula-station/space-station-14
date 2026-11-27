// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Server/StationGoal/StationGoalComponent.cs

using Content.Shared._Arcane.StationGoal;
using Robust.Shared.Prototypes;

namespace Content.Server._Arcane.StationGoal;

/// <summary>
///     Added to a station, picks a random goal from the list at round start.
/// </summary>
[RegisterComponent, Access(typeof(StationGoalSystem))]
public sealed partial class StationGoalComponent : Component
{
    [DataField]
    public List<ProtoId<StationGoalPrototype>> Goals = [];
}
