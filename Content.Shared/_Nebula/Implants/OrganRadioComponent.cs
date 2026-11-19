// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.Implants;

/// <summary>
/// Organ with an encryption key holder that gives the body intrinsic radio on the channels of its keys while enabled.
/// Keys are swapped before installation, the holder is not reachable inside the body.
/// </summary>
[RegisterComponent]
public sealed partial class OrganRadioComponent : Component
{
    [ViewVariables]
    public HashSet<ProtoId<RadioChannelPrototype>> AddedReceiverChannels = [];

    [ViewVariables]
    public HashSet<ProtoId<RadioChannelPrototype>> AddedTransmitterChannels = [];

    [ViewVariables]
    public bool AddedReceiver;
}

/// <summary>
/// Body with an enabled <see cref="OrganRadioComponent"/> organ, used for the default channel prefix.
/// </summary>
[RegisterComponent]
public sealed partial class OrganRadioUserComponent : Component
{
    [ViewVariables]
    public EntityUid Organ;
}
