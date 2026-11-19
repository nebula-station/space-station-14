// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Nebula.Security.SecDispatch;

[DataDefinition]
public sealed partial class SecDispatchEntry
{
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Radio message, gets the nearest beacon name as <c>$location</c>.
    /// </summary>
    [DataField(required: true)]
    public LocId Message;

    [DataField(required: true)]
    public SpriteSpecifier Icon = SpriteSpecifier.Invalid;

    /// <summary>
    /// Highlights the option in the menu.
    /// </summary>
    [DataField]
    public bool Combat;

    /// <summary>
    /// Played to every player whose worn headset received this report.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;
}

[Serializable, NetSerializable]
public enum SecDispatchUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SecDispatchMessage(int index) : BoundUserInterfaceMessage
{
    public int Index { get; } = index;
}

public sealed partial class SecDispatchActionEvent : InstantActionEvent;
