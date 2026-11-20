// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Destructible.Thresholds;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Nebula.GameTicking.Rules;

/// <summary>
/// Every <see cref="Delay"/> seconds the headset of a random living security member plays a reminder,
/// loud enough for everyone within <see cref="Range"/>.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SecurityReminderRuleComponent : Component
{
    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    /// <summary>
    /// Seconds between two reminders.
    /// </summary>
    [DataField]
    public MinMax Delay = new(5 * 60, 15 * 60);

    [DataField]
    public float Range = 4f;

    /// <summary>
    /// Headsets that receive this channel count as security ones.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> Channel = "Security";

    [ViewVariables, AutoPausedField]
    public TimeSpan NextReminder;
}
