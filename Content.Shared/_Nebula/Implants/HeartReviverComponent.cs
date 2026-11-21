// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Shared._Nebula.Implants;

/// <summary>
/// Heart implant that heals a dead body after a delay and defibrillates it at the cost of the heart's integrity.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class HeartReviverComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier Healing = new();

    [DataField(readOnly: true)]
    public TimeSpan StartDelay = TimeSpan.FromMinutes(2);

    [DataField(readOnly: true)]
    public TimeSpan Interval = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The implant gives up on a body with more vital damage than this.
    /// </summary>
    [DataField(readOnly: true)]
    public FixedPoint2 MaxDamage = 500;

    /// <summary>
    /// The body is defibrillated once its vital damage drops to this.
    /// </summary>
    [DataField(readOnly: true)]
    public FixedPoint2 ReviveDamage = 80;

    /// <summary>
    /// Part of the heart's integrity cap lost on every revive.
    /// </summary>
    [DataField(readOnly: true)]
    public float HeartIntegrityCost = 0.4f;

    [DataField(readOnly: true)]
    public SoundSpecifier ReviveSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_zap.ogg");

    [ViewVariables, AutoPausedField]
    public TimeSpan NextUpdate;

    [ViewVariables, AutoPausedField]
    public TimeSpan? DeadSince;
}

/// <summary>
/// Put on the body by a <see cref="HeartReviverComponent"/> to remember a suicide.
/// </summary>
[RegisterComponent]
public sealed partial class HeartReviverHostComponent : Component
{
    [ViewVariables]
    public bool Suicided;
}
