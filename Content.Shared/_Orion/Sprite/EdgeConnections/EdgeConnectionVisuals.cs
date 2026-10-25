// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/02837fb57c50d53cddaeab30fe0e12477e02efda/Content.Shared/_Orion/Sprite/EdgeConnections/EdgeConnectionVisuals.cs

using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Sprite.EdgeConnections;

[Serializable, NetSerializable]
public enum EdgeConnectionVisuals : byte
{
    ConnectionMask,
}

[Flags]
[Serializable, NetSerializable]
public enum EdgeConnectionDirections : byte
{
    None = 0,
    North = 1 << 0,
    South = 1 << 1,
    East = 1 << 2,
    West = 1 << 3,
}
