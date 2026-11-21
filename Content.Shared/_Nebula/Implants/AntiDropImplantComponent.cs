// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.Implants;

/// <summary>
/// Hand implant that locks the item held by its own hand while toggled on.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AntiDropImplantComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Locked;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? Action;
}

/// <summary>
/// Item locked in a hand by an <see cref="AntiDropImplantComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AntiDropLockedComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid Implant;

    /// <summary>
    /// Whether the lock added the item's <see cref="Interaction.Components.UnremoveableComponent"/> and has to remove it.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool AddedUnremoveable;
}

public sealed partial class ToggleAntiDropEvent : InstantActionEvent;
