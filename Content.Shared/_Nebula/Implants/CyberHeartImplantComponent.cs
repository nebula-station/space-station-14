// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.Implants;

/// <summary>
/// Heart implant that periodically heals the body while the organ is enabled.
/// When an EMP disables it, the body gets <see cref="CyberHeartEmpComponent"/> until it recovers.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CyberHeartImplantComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier Healing = new();

    [DataField(readOnly: true)]
    public TimeSpan Interval = TimeSpan.FromSeconds(8);

    [ViewVariables, AutoPausedField]
    public TimeSpan NextHealing;
}

/// <summary>
/// Body effect of EMP-disabled heart implants: slowdown, heart pain and random shocks.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CyberHeartEmpComponent : Component
{
    /// <summary>
    /// Disabled implants causing the effect. It ends when the set is empty.
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> Sources = [];

    [DataField, AutoNetworkedField]
    public float SpeedModifier = 0.8f;

    [ViewVariables, AutoPausedField]
    public TimeSpan NextShock;

    [ViewVariables, AutoPausedField]
    public TimeSpan NextPainPopup;
}
