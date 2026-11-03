// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/93847a07a4b0b5890003c074e4768c5ba828543a/Content.Arcane.Shared/Medical/Concussion/ConcussionThresholdComponent.cs

using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Arcane.Medical.Concussion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true, fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class ConcussionThresholdComponent : Component
{
    [DataField, AutoNetworkedField]
    public ConcussionState CurrentState = ConcussionState.Sane;

    [DataField, AutoNetworkedField]
    public FixedPoint2 StoredDamage;

    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, ConcussionState> Thresholds = [];

    [DataField]
    public HashSet<ConcussionState> AllowedStates =
    [
        ConcussionState.Sane, ConcussionState.Minor, ConcussionState.Hard, ConcussionState.Overwhelmed,
    ];

    [DataField]
    public Dictionary<ConcussionState, float> SpeedModifierThresholds = [];

    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Fraction of maximum concussion intensity below which hearing starts returning.
    /// </summary>
    [DataField]
    public float HearingRecoveryThreshold = 0.3f;

    [DataField]
    public float HealRate = 1f;

    [DataField]
    public FixedPoint2 FallbackDamageCap = 200;

    [DataField]
    public float ExplosionDamageMultiplier = 10f;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ConcussedComponent : Component;

// Nebula start
[RegisterComponent]
public sealed partial class ActiveConcussionComponent : Component;
// Nebula end

[RegisterComponent]
public sealed partial class ConcussionResistanceComponent : Component
{
    [DataField]
    public float Resistance;
}
