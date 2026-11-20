// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.Squads;

/// <summary>
/// Voice command the head of security plays from the squad tablet into the headsets of one squad.
/// </summary>
[Prototype]
public sealed partial class SquadVoiceCommandPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Shown on hover over the command button.
    /// </summary>
    [DataField(required: true)]
    public LocId Description;

    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    /// <summary>
    /// Position in the tablet list.
    /// </summary>
    [DataField]
    public int Order;
}
