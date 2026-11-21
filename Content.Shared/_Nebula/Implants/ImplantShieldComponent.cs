// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.Implants;

/// <summary>
/// Arm implant that keeps a hardlight shield and deploys it into the hand of its own side.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ImplantShieldArmComponent : Component
{
    public const string ContainerId = "implant_shield";

    [DataField(required: true)]
    public EntProtoId Shield;

    /// <summary>
    /// How long the shield can't be deployed after it breaks.
    /// </summary>
    [DataField(readOnly: true)]
    public TimeSpan BrokenCooldown = TimeSpan.FromMinutes(4);

    [ViewVariables, AutoNetworkedField]
    public EntityUid? ShieldEntity;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? Action;
}

/// <summary>
/// Shield of an <see cref="ImplantShieldArmComponent"/>. It retracts once it takes <see cref="BreakDamage"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ImplantShieldComponent : Component
{
    [DataField(readOnly: true)]
    public FixedPoint2 BreakDamage = 75;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? Arm;
}

public sealed partial class ToggleImplantShieldEvent : InstantActionEvent;
