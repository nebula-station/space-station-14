// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Shared/_LuaM/Cyberware/BulletAbsorbComponent.cs

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._LuaM.Cyberware;

/// <summary>
/// Fully absorbs the damage of one bullet, then needs <see cref="Cooldown"/> to be ready again.
/// Put on the body through an armor organ's onAdd.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class BulletAbsorbComponent : Component
{
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(20);

    /// <summary>
    /// When the next bullet can be absorbed.
    /// </summary>
    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextAbsorb;

    [DataField]
    public SoundSpecifier? AbsorbSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/ric1.ogg");

    [DataField]
    public LocId AbsorbPopup = "bullet-absorb-popup";

    /// <summary>
    /// Shown to the owner when the armor is ready again.
    /// </summary>
    [DataField]
    public LocId ReadyPopup = "bullet-absorb-ready";
}

/// <summary>
/// Marks a body whose bullet-absorbing armor is recharging, so only those are checked for the ready popup.
/// </summary>
[RegisterComponent]
public sealed partial class BulletAbsorbRechargingComponent : Component;
