// SPDX-License-Identifier: AGPL-3.0-only
// Ported from https://github.com/AdventureTimeSS14/space_station_ADT/blob/296c8b93290412fb8b446c0f42ce107851eefedf/Content.Shared/ADT/Shuttles/Components/DropPodBeaconInfo.cs

using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._ADT.Shuttles.Components;

/// <summary>
/// Identifies a valid (non-blacklisted) landing beacon sent to the client UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class DropPodBeaconInfo
{
    public NetEntity Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    /// <summary>World-space position used to highlight this beacon on the nav map.</summary>
    public Vector2 WorldPos { get; init; }
}
