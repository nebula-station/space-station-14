// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.Implants;

/// <summary>
/// Security tracking implant that raises an alert once its owner goes into crit.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SecurityTrackingImplantComponent : Component
{
    [DataField(readOnly: true)]
    public ProtoId<RadioChannelPrototype> Channel = "Security";

    [DataField(readOnly: true)]
    public LocId Message = "rattle-on-trigger-critical-message";

    /// <summary>
    /// Played to everyone who received the message on <see cref="Channel"/>.
    /// </summary>
    [DataField(readOnly: true)]
    public SoundSpecifier AlertSound = new SoundPathSpecifier("/Audio/_Nebula/Security/sec_implant_alert.ogg");

    [DataField(readOnly: true)]
    public SoundSpecifier FlatlineSound = new SoundPathSpecifier("/Audio/_Nebula/Security/sec_implant_flatline.ogg");

    [DataField(readOnly: true)]
    public float FlatlineRange = 5f;

    [DataField(readOnly: true)]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    [DataField(readOnly: true)]
    public TimeSpan MarkDuration = TimeSpan.FromSeconds(10);

    [ViewVariables, AutoPausedField]
    public TimeSpan NextAlert;
}

/// <summary>
/// Sent to owners of a neural interface, shows a mark and a task where the implant owner went into crit.
/// <see cref="Location"/> is the name of the nearest beacon.
/// </summary>
[Serializable, NetSerializable]
public sealed class SecurityImplantCritMarkEvent(NetCoordinates coordinates, TimeSpan duration, string name, string location)
    : EntityEventArgs
{
    public NetCoordinates Coordinates { get; } = coordinates;
    public TimeSpan Duration { get; } = duration;
    public string Name { get; } = name;
    public string Location { get; } = location;
}
