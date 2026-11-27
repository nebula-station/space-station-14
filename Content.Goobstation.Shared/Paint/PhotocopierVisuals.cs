// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Goobstation.Shared/Paint/PhotocopierVisuals.cs

using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Paint;

[Serializable, NetSerializable]
public enum PhotocopierVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum PhotocopierState : byte
{
    Idle,
    Scanning,
    Printing,
}
