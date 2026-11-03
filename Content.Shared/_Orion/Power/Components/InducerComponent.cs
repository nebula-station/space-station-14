// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/5406a8fe0da141f24b2b1e842714a7704b37aba3/Content.Shared/_Orion/Power/Components/InducerComponent.cs

using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Power.Components;

/// <summary>
/// Transfers energy from the power cell in its slot to the battery of a clicked entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InducerComponent : Component
{
    /// <summary>
    /// Joules per second drawn from the cell.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int TransferRate;

    [DataField]
    public List<int> AvailableTransferRates = [];

    /// <summary>
    /// Seconds per transfer step, one doafter each.
    /// </summary>
    [DataField]
    public float TransferDelay;

    /// <summary>
    /// Multiplier for energy received by loose entities (weapons, batteries, clothing).
    /// </summary>
    [DataField]
    public float TransferMultiplier;

    /// <summary>
    /// Multiplier for energy received by anchored entities (machines, APCs, wall mounts).
    /// </summary>
    [DataField]
    public float StructureTransferMultiplier;

    [DataField]
    public float MaxDistance;
}

[Serializable, NetSerializable]
public sealed partial class InducerDoAfterEvent : SimpleDoAfterEvent;
