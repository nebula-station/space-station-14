// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Server/Anomaly/WormholeAnomalyComponent.cs

using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Arcane.Anomaly;

/// <summary>
/// Periodically teleports the entity to a random nearby position.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class WormholeAnomalyComponent : Component
{
    /// <summary>
    /// Time between teleports.
    /// </summary>
    [DataField]
    public TimeSpan PulseInterval = TimeSpan.FromSeconds(15);

    /// <summary>
    /// The maximum teleport distance.
    /// </summary>
    [DataField]
    public float MaxShuffleRadius = 12f;

    /// <summary>
    /// Played after teleporting.
    /// </summary>
    [DataField]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPulse;
}
