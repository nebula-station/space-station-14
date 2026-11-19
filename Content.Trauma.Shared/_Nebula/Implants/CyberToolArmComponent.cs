// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared._Nebula.Implants;

/// <summary>
/// Cybernetic arm with built-in tools, picked through the augment tool panel radial.
/// The storage only accepts the arm's own tools, and pressing the action again retracts the deployed one.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CyberToolArmComponent : Component
{
    /// <summary>
    /// Tools spawned into the arm's storage on map init.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Tools = [];
}

/// <summary>
/// Marks a tool built into a <see cref="CyberToolArmComponent"/> arm.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CyberToolArmItemComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Arm;
}
