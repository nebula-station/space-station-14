// SPDX-License-Identifier: AGPL-3.0-only
// Ported from https://github.com/AdventureTimeSS14/space_station_ADT/blob/296c8b93290412fb8b446c0f42ce107851eefedf/Content.Shared/ADT/Shuttles/Components/DropPodConsoleDeployMessage.cs

using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._ADT.Shuttles.Components;

/// <summary>
/// Sent by the client to request launching toward a specific beacon.
/// The server applies a random offset so the exact tile is never revealed in advance.
/// </summary>
[Serializable, NetSerializable]
public sealed class DropPodConsoleDeployMessage : BoundUserInterfaceMessage
{
    public NetEntity TargetBeacon { get; init; }
}
