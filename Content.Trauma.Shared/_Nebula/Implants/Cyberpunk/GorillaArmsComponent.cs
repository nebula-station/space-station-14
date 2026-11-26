// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

/// <summary>
/// Owner of the gorilla combat arms: heavy punches and a hard grab now and then.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class GorillaArmsComponent : Component
{
    /// <summary>
    /// Damage of an unarmed hit, replacing the usual one.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier UnarmedDamage = new();

    [DataField(readOnly: true)]
    public TimeSpan HardGrabCooldown = TimeSpan.FromMinutes(1);

    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextHardGrab;
}

/// <summary>
/// Arm that dashes its owner forward and slams the floor around the landing spot.
/// </summary>
[RegisterComponent]
public sealed partial class GorillaSlamComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    [DataField(readOnly: true)]
    public float Range = 3f;
}

/// <summary>
/// Body in the middle of a gorilla slam dash, slamming once it lands.
/// </summary>
[RegisterComponent]
public sealed partial class GorillaSlammingComponent : Component
{
    [ViewVariables]
    public EntityUid Arm;
}
