// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Squads;
using Content.Shared.StationRecords;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Nebula.Squads;

/// <summary>
/// Security squads of a station and their orders, managed by the head of security.
/// </summary>
[RegisterComponent, Access(typeof(SecuritySquadSystem))]
public sealed partial class StationSquadsComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public List<SecuritySquad> Squads = [];

    /// <summary>
    /// Active orders of all squads, oldest first.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public List<SquadOrder> Orders = [];

    /// <summary>
    /// Tasks from the head of security, oldest first.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public List<SquadTask> Tasks = [];

    [ViewVariables(VVAccess.ReadOnly)]
    public int NextId;

    [ViewVariables(VVAccess.ReadOnly)]
    public int NextTaskId;

    [ViewVariables(VVAccess.ReadOnly)]
    public int NextOrderId;
}

public sealed class SecuritySquad
{
    public required int Id;
    public required string Name;
    public required Color Color;

    /// <summary>
    /// Members are tracked by station record, so a member stays in the squad while their body changes.
    /// </summary>
    public readonly List<StationRecordKey> Members = [];

    public TimeSpan NextVoiceCommand;
}

/// <summary>
/// <see cref="Coordinates"/> are relative to the grid the order was placed on,
/// <see cref="Location"/> is the name of the beacon nearest to them.
/// </summary>
public sealed class SquadOrder
{
    public required int Id;
    public required int Squad;
    public required ProtoId<SquadOrderPrototype> Order;
    public required EntityCoordinates Coordinates;
    public required string Location;
    public required TimeSpan ExpiresAt;
}

/// <summary>
/// Task for a whole squad when <see cref="Squad"/> is set, for one member when <see cref="Member"/> is.
/// </summary>
public sealed class SquadTask
{
    public required int Id;
    public int? Squad;
    public StationRecordKey? Member;
    public required string Title;
    public required string Description;
}
