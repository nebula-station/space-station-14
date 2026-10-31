// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.Squads;

[Serializable, NetSerializable]
public enum SquadTabletUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum SquadMemberSignal : byte
{
    None,
    Alive,
    Dead,
}

public static class SquadLimits
{
    public const int MaxSquads = 6;
    public const int MaxNameLength = 24;
    public const int MaxOrdersPerSquad = 5;
    public const int MaxTitleLength = 48;
    public const int MaxDescriptionLength = 512;
    public const int MaxPersonalTasks = 10;

    /// <summary>
    /// Head of security tasks on one station, squad and personal ones together.
    /// </summary>
    public const int MaxTasks = 40;
}

public static class SquadOrders
{
    /// <summary>
    /// Orders go on a tile of the station grid, not into the space around its hull.
    /// </summary>
    public static bool IsOnStationTile(SharedMapSystem map, Entity<MapGridComponent> grid, EntityCoordinates coordinates)
    {
        return map.TryGetTileRef(grid, grid.Comp, coordinates, out var tile)
            && !tile.Tile.IsEmpty;
    }
}

public static class SquadPalette
{
    /// <summary>
    /// Squad colors, one squad per color. None of them is close to the state colors (cyan alive, yellow crit,
    /// red dead), so a squad line never reads as a state.
    /// </summary>
    public static readonly Color[] Colors =
    [
        Color.FromHex("#ff2fa5"),
        Color.FromHex("#a259ff"),
        Color.FromHex("#ff7a00"),
        Color.FromHex("#8aff00"),
        Color.FromHex("#f2f2f2"),
        Color.FromHex("#c9a7ff"),
    ];
}

/// <summary>
/// <see cref="Coordinates"/> comes from the suit sensor and is null when it does not report it.
/// <see cref="InCombat"/> is set for a few seconds after the member took heavy damage in a short time.
/// </summary>
[Serializable, NetSerializable]
public struct SquadTabletMember
{
    public required uint Record;
    public required string Name;
    public required string Job;
    public NetEntity? Entity;
    public SquadMemberSignal Signal;
    public bool InCombat;
    public NetCoordinates? Coordinates;
}

[Serializable, NetSerializable]
public struct SquadTabletSquad
{
    public required int Id;
    public required string Name;
    public required Color Color;
    public required List<SquadTabletMember> Members;
}

/// <summary>
/// <see cref="Coordinates"/> are always relative to the grid the order was placed on.
/// </summary>
[Serializable, NetSerializable]
public struct SquadTabletOrder
{
    public required int Id;
    public required int Squad;
    public required ProtoId<SquadOrderPrototype> Order;
    public required NetCoordinates Coordinates;
}

/// <summary>
/// A task for a whole squad has <see cref="Squad"/> set, a personal one has <see cref="Record"/>.
/// </summary>
[Serializable, NetSerializable]
public struct SquadTabletTask
{
    public required int Id;
    public int? Squad;
    public uint? Record;
    public required string Title;
    public required string Description;
}

/// <summary>
/// A running brig timer. <see cref="End"/> is in game time.
/// </summary>
[Serializable, NetSerializable]
public struct SquadTabletTimer
{
    public required NetEntity Entity;
    public required string Name;
    public required TimeSpan End;
}

[Serializable, NetSerializable]
public sealed class SquadTabletState(
    List<SquadTabletSquad> squads,
    List<SquadTabletMember> free,
    List<SquadTabletOrder> orders,
    List<SquadTabletTask> tasks,
    List<SquadTabletTimer> timers)
    : BoundUserInterfaceState
{
    public List<SquadTabletSquad> Squads { get; } = squads;
    public List<SquadTabletMember> Free { get; } = free;
    public List<SquadTabletOrder> Orders { get; } = orders;
    public List<SquadTabletTask> Tasks { get; } = tasks;
    public List<SquadTabletTimer> Timers { get; } = timers;

    /// <summary>
    /// The tablet rebuilds the state on every refresh, comparing by value keeps an unchanged one from being sent.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not SquadTabletState other
            || Squads.Count != other.Squads.Count
            || !Free.SequenceEqual(other.Free)
            || !Orders.SequenceEqual(other.Orders)
            || !Tasks.SequenceEqual(other.Tasks)
            || !Timers.SequenceEqual(other.Timers))
            return false;

        for (var i = 0; i < Squads.Count; i++)
        {
            var a = Squads[i];
            var b = other.Squads[i];
            if (a.Id != b.Id
                || a.Name != b.Name
                || a.Color != b.Color
                || !a.Members.SequenceEqual(b.Members))
                return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Squads.Count, Free.Count, Orders.Count, Tasks.Count, Timers.Count);
    }
}

[Serializable, NetSerializable]
public sealed class SquadTabletCreateSquadMessage(string name, int color, List<uint> members) : BoundUserInterfaceMessage
{
    public string Name { get; } = name;

    /// <summary>
    /// Records dropped into the new squad before it was created.
    /// </summary>
    public List<uint> Members { get; } = members;

    /// <summary>
    /// Index in <see cref="SquadPalette.Colors"/>.
    /// </summary>
    public int Color { get; } = color;
}

[Serializable, NetSerializable]
public sealed class SquadTabletDeleteSquadMessage(int squad) : BoundUserInterfaceMessage
{
    public int Squad { get; } = squad;
}

/// <summary>
/// Moves the member to the squad, a null squad makes them free.
/// </summary>
[Serializable, NetSerializable]
public sealed class SquadTabletAssignMessage(uint record, int? squad) : BoundUserInterfaceMessage
{
    public uint Record { get; } = record;
    public int? Squad { get; } = squad;
}

[Serializable, NetSerializable]
public sealed class SquadTabletPlaceOrderMessage(int squad, ProtoId<SquadOrderPrototype> order, NetCoordinates coordinates)
    : BoundUserInterfaceMessage
{
    public int Squad { get; } = squad;
    public ProtoId<SquadOrderPrototype> Order { get; } = order;
    public NetCoordinates Coordinates { get; } = coordinates;
}

[Serializable, NetSerializable]
public sealed class SquadTabletCancelOrderMessage(int order) : BoundUserInterfaceMessage
{
    public int Order { get; } = order;
}

/// <summary>
/// Gives a task to the whole squad or, with <see cref="Record"/> set, to one member.
/// </summary>
[Serializable, NetSerializable]
public sealed class SquadTabletCreateTaskMessage(int? squad, uint? record, string title, string description)
    : BoundUserInterfaceMessage
{
    public int? Squad { get; } = squad;
    public uint? Record { get; } = record;
    public string Title { get; } = title;
    public string Description { get; } = description;
}

[Serializable, NetSerializable]
public sealed class SquadTabletDeleteTaskMessage(int task) : BoundUserInterfaceMessage
{
    public int Task { get; } = task;
}

/// <summary>
/// Sent to open tablets when a brig timer of their station runs out.
/// </summary>
[Serializable, NetSerializable]
public sealed class SquadTabletTimerFinishedMessage(string name) : BoundUserInterfaceMessage
{
    public string Name { get; } = name;
}
