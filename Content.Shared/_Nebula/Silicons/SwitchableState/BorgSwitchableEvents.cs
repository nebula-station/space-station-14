// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Nebula.Silicons.SwitchableState;

[Serializable, NetSerializable]
public sealed class SwitchStateMessage(BorgStateType type) : BoundUserInterfaceMessage
{
    public BorgStateType Type = type;
}

[Serializable, NetSerializable]
public enum SwitchStateUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum SwitchStateVisuals : byte
{
    Key,
}

public sealed partial class SwitchStateActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class SwitchStateDoAfterEvent : DoAfterEvent
{
    [DataField(required: true)] public BorgStateType Type;

    public SwitchStateDoAfterEvent(BorgStateType type)
    {
        Type = type;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
