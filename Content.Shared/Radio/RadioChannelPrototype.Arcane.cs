// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Shared/_Arcane/Radio/RadioChannelPrototype.cs

using Robust.Shared.Audio;

namespace Content.Shared.Radio;

public sealed partial class RadioChannelPrototype
{
    /// <summary>
    /// Played to headset wearers when a message arrives on this channel. Falls back to a generic bark.
    /// </summary>
    [DataField]
    public SoundPathSpecifier? OnSendSound;
}
