// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Shared/_Orion/Bitrunning/BitrunningVisuals.cs

using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Bitrunning;

[Serializable, NetSerializable]
public enum QuantumServerVisualState : byte
{
    Unpowered,
    Ready,
    Cooling,
    Running,
}

[Serializable, NetSerializable]
public enum QuantumServerVisuals : byte
{
    QuantumServerState,
}

[Serializable, NetSerializable]
public enum ByteforgeVisuals : byte
{
    ByteforgePowered,
    ByteforgeActive,
    ByteforgeAngry,
}
