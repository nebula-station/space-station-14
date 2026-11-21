// SPDX-License-Identifier: AGPL-3.0-only
// Ported from https://github.com/AdventureTimeSS14/space_station_ADT/blob/296c8b93290412fb8b446c0f42ce107851eefedf/Content.Shared/ADT/Shuttles/Components/DropPodTargetStationComponent.cs

using Robust.Shared.GameStates;

namespace Content.Shared._ADT.Shuttles.Components;

/// <summary>
/// Marks a station as the target for drop pod launches.
/// Only beacons on grids belonging to this station will be shown in the drop pod console.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DropPodTargetStationComponent : Component
{
}
