// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

/// <summary>
/// Head implant that makes a chosen recharging implant ready at once.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class QuantumRegulatorComponent : Component
{
    [DataField(readOnly: true)]
    public TimeSpan Cooldown = TimeSpan.FromMinutes(10);
}

public sealed partial class QuantumRegulatorActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public enum QuantumRegulatorUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class QuantumRegulatorSelectMessage(NetEntity implant) : BoundUserInterfaceMessage
{
    public NetEntity Implant { get; } = implant;
}
