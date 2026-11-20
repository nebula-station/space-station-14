// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Server/_Goobstation/Sandevistan/SandevistanUserComponent.cs

using Content.Shared.Abilities;
using Content.Shared.Damage;
using Content.Trauma.Common.Wizard.Projectile;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._LuaM.Sandevistan;

/// <summary>
/// Sandevistan of a CNS implant, put on the body through the organ's onAdd.
/// It runs until turned off: after the safe running time it starts hurting the user every second.
/// </summary>
[RegisterComponent]
public sealed partial class CnsSandevistanUserComponent : Component
{
    [ViewVariables]
    public bool Active;

    /// <summary>
    /// Time when the safe running time of the active Sandevistan ends.
    /// </summary>
    [ViewVariables]
    public TimeSpan? OverloadAt;

    /// <summary>
    /// Time of the next overload damage tick.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextOverloadDamage;

    /// <summary>
    /// Whether the user was already warned about the overload during this activation.
    /// </summary>
    [ViewVariables]
    public bool OverloadWarned;

    /// <summary>
    /// Whether the user was already warned that the safe running time is about to end.
    /// </summary>
    [ViewVariables]
    public bool PreOverloadWarned;

    /// <summary>
    /// How long before the end of the safe running time the user gets warned.
    /// </summary>
    [DataField]
    public TimeSpan OverloadWarningTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Sound played to the user together with the warnings.
    /// </summary>
    [DataField]
    public SoundSpecifier? OverloadWarningSound = new SoundPathSpecifier("/Audio/Effects/alert.ogg");

    /// <summary>
    /// Strength of the glitch screen effect during the safe running time.
    /// </summary>
    [DataField]
    public float GlitchIntensity = 0.4f;

    /// <summary>
    /// Strength of the glitch screen effect once the Sandevistan is overloaded.
    /// </summary>
    [DataField]
    public float OverloadGlitchIntensity = 1f;

    /// <summary>
    /// Damage dealt every <see cref="OverloadInterval"/> after the safe running time.
    /// </summary>
    [DataField]
    public DamageSpecifier OverloadDamage = new()
    {
        DamageDict = new() { { "Cellular", 15 } },
    };

    [DataField]
    public TimeSpan OverloadInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// If set, dealt instead of <see cref="OverloadDamage"/> once the implant has been running
    /// for <see cref="HeavyOverloadAfter"/>.
    /// </summary>
    [DataField]
    public DamageSpecifier? HeavyOverloadDamage;

    [DataField]
    public TimeSpan HeavyOverloadAfter = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Shown to the user <see cref="OverloadWarningTime"/> before the heavy damage starts. No warning if null.
    /// </summary>
    [DataField]
    public LocId? HeavyOverloadWarning;

    [ViewVariables]
    public bool HeavyOverloadWarned;

    /// <summary>
    /// When the implant was turned on.
    /// </summary>
    [ViewVariables]
    public TimeSpan ActivatedAt;

    /// <summary>
    /// Time when the Sandevistan will finish recharging and become usable again.
    /// </summary>
    [ViewVariables]
    public TimeSpan? RechargeAt;

    /// <summary>
    /// How long the Sandevistan can run safely after activation, after that it deals <see cref="OverloadDamage"/>.
    /// </summary>
    [DataField]
    public TimeSpan SafeDuration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long the Sandevistan must recharge after being disabled.
    /// </summary>
    [DataField]
    public TimeSpan RechargeDuration = TimeSpan.FromSeconds(40);

    [DataField]
    public EntProtoId ActionProto = "ActionToggleCnsSandevistan";

    /// <summary>
    /// Whether guns are blocked while running. The Kerenzikov lets its user shoot.
    /// </summary>
    [DataField]
    public bool BlockGuns = true;

    /// <summary>
    /// Whether the user gets the "safe mode ends" and "overloaded" warnings.
    /// Off for implants that hurt their user the whole time they run.
    /// </summary>
    [DataField]
    public bool OverloadWarnings = true;

    [ViewVariables]
    public EntityUid? ActionUid;

    [DataField]
    public float MovementSpeedModifier = 2f;

    [DataField]
    public float AttackSpeedModifier = 2f;

    [DataField]
    public SoundSpecifier? StartSound = new SoundPathSpecifier("/Audio/_Trauma/Misc/sande_start.ogg");

    [DataField]
    public SoundSpecifier? EndSound = new SoundPathSpecifier("/Audio/_Trauma/Misc/sande_end.ogg");

    [ViewVariables]
    public EntityUid? RunningSound;

    [ViewVariables]
    public DogVisionComponent? Overlay;

    [ViewVariables]
    public TrailComponent? Trail;

    /// <summary>
    /// When the trail color changes next. It is networked, so it isn't changed every tick.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextTrailColor;
}
