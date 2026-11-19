// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Shared/_LuaM/Sandevistan/SandevistanGunBlockComponent.cs

using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared._LuaM.Sandevistan;

/// <summary>
/// Added while a CNS sandevistan is running: the user can't shoot guns, only fight in melee.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SandevistanGunBlockComponent : Component
{
    [DataField]
    public TimeSpan PopupCooldown = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public TimeSpan NextPopup;
}

public sealed partial class ToggleCnsSandevistanEvent : InstantActionEvent;
