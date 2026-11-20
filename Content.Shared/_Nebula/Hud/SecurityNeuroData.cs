// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Squads;
using Content.Shared.Mobs;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.Hud;

/// <summary>
/// Task sources, in HUD priority order after the orders on the map.
/// </summary>
[Serializable, NetSerializable]
public enum SecurityNeuroTaskKind : byte
{
    Order,
    Personal,
    Wanted,
}

[Serializable, NetSerializable]
public readonly record struct SecurityNeuroTaskKey(SecurityNeuroTaskKind Kind, int Id);

[Serializable, NetSerializable]
public record struct SecurityNeuroSquadMember
{
    public required string Name;
    public required MobState State;

    /// <summary>
    /// The first member of the squad leads it.
    /// </summary>
    public bool Leader;
}

/// <summary>
/// <see cref="Location"/> is the name of the beacon nearest to the order.
/// </summary>
[Serializable, NetSerializable]
public record struct SecurityNeuroMark
{
    public required int Id;
    public required ProtoId<SquadOrderPrototype> Order;
    public required string Location;
    public required Color Color;
    public required NetCoordinates Coordinates;
}

/// <summary>
/// The description is not shown on the HUD, only marked there.
/// </summary>
[Serializable, NetSerializable]
public record struct SecurityNeuroTask
{
    public required SecurityNeuroTaskKey Key;
    public required string Title;
    public required string Description;
    public required bool Pinned;
}

public struct SecurityNeuroPersonalTask
{
    public required int Id;
    public required string Title;
}

[Serializable, NetSerializable]
public record struct SecurityNeuroSquadSummary
{
    public required string Name;
    public required Color Color;
    public required int Alive;
    public required int Total;

    /// <summary>
    /// Whether anyone in the squad is in crit or dead.
    /// </summary>
    public required bool Losses;
}

[Serializable, NetSerializable]
public sealed class SecurityNeuroTaskAddEvent(string title) : EntityEventArgs
{
    public string Title { get; } = title;
}

/// <summary>
/// Removes a personal task, others cannot be removed by their receiver.
/// </summary>
[Serializable, NetSerializable]
public sealed class SecurityNeuroTaskRemoveEvent(int id) : EntityEventArgs
{
    public int Id { get; } = id;
}

[Serializable, NetSerializable]
public sealed class SecurityNeuroTaskPinEvent(SecurityNeuroTaskKey key, bool pinned) : EntityEventArgs
{
    public SecurityNeuroTaskKey Key { get; } = key;
    public bool Pinned { get; } = pinned;
}

/// <summary>
/// Sent to the commander when a squad member falls into crit or dies.
/// </summary>
[Serializable, NetSerializable]
public sealed class SecurityNeuroLossEvent(string name, string squad, MobState state) : EntityEventArgs
{
    public string Name { get; } = name;
    public string Squad { get; } = squad;
    public MobState State { get; } = state;
}

/// <summary>
/// Sent to the commander when a brig timer of their station runs out.
/// </summary>
[Serializable, NetSerializable]
public sealed class SecurityNeuroTimerEvent(string name) : EntityEventArgs
{
    public string Name { get; } = name;
}
