// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Common/Radio/RadioBarkEvent.cs

using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Arcane.Radio;

/// <summary>
/// Tells a client to play the bark of a radio message it just received (Orion #452, #456).
/// </summary>
[Serializable, NetSerializable]
public sealed class RadioBarkEvent(ResPath path, AudioParams audioParams) : EntityEventArgs
{
    public readonly ResPath Path = path;
    public readonly AudioParams Params = audioParams;
}
