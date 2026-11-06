// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.MartialArts;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Nebula.Movement.Pulling;

/// <summary>
/// Gloves that start a combat mode pull at this grab stage instead of a soft grab.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class StartingGrabStageComponent : Component
{
    [DataField]
    public GrabStage Stage = GrabStage.Soft;

    /// <summary>
    /// Time after a boosted grab during which new grabs start soft.
    /// </summary>
    [DataField(readOnly: true)]
    public TimeSpan Cooldown = TimeSpan.Zero;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextBoostedGrab;
}
