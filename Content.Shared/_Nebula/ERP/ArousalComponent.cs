// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Nebula.ERP;

public enum ArousalPhase : byte
{
    Calm,
    Interested,
    Aroused,
    Heated,
    Peak,
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ArousalComponent : Component
{
    /// <summary>
    /// Only the owner and whoever has the panel open on them get the state, see <see cref="ArousalSystem"/>.
    /// </summary>
    public override bool SessionSpecific => true;

    [AutoNetworkedField]
    public float LastValue;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan LastChangeTime;

    [DataField, AutoNetworkedField]
    public float DecayRate = 0.3f;

    [DataField]
    public float MaxArousal = 100f;

    /// <summary>
    /// Server-only scheduler for phase and alert updates.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPhaseCheckAt;

    [AutoNetworkedField]
    public ArousalPhase CurrentPhase;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan RefractoryUntil;

    [DataField]
    public TimeSpan RefractoryDuration = TimeSpan.FromSeconds(60);

    public ArousalPhase ComputePhase(float arousal) => arousal switch
    {
        < 20f => ArousalPhase.Calm,
        < 40f => ArousalPhase.Interested,
        < 70f => ArousalPhase.Aroused,
        < 100f => ArousalPhase.Heated,
        _ => ArousalPhase.Peak,
    };
}
