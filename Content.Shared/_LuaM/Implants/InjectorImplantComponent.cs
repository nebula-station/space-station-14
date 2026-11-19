// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Shared/_LuaM/Implants/InjectorImplantComponent.cs

using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Audio;

namespace Content.Shared._LuaM.Implants;

/// <summary>
/// An implant that injects a solution into its owner from a hotbar action.
/// The charges of the action recharge by themselves, and it can inject itself when the owner goes critical.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class InjectorImplantComponent : Component
{
    /// <summary>
    /// What gets injected into the bloodstream on every use.
    /// </summary>
    [DataField(required: true)]
    public Solution Solution = new();

    /// <summary>
    /// Whether the implant injects automatically when its owner becomes critical.
    /// </summary>
    [DataField]
    public bool InjectOnCritical = true;

    [DataField]
    public LocId UsedPopup = "injector-implant-used";

    /// <summary>
    /// Minimum time between automatic injections, so repeated crits do not drain every charge.
    /// </summary>
    [DataField]
    public TimeSpan AutoInjectCooldown = TimeSpan.FromSeconds(60);

    /// <summary>
    /// When the implant can inject automatically again.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan NextAutoInject;

    [DataField]
    public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");
}

public sealed partial class UseInjectorImplantEvent : InstantActionEvent;
