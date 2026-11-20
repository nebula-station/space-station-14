// SPDX-License-Identifier: AGPL-3.0-or-later

// Ported from https://github.com/Goob-Station/Goob-Station/blob/707e36693d1d9289a4f04921c5e070b1e4195589/Content.Shared/_ES/Camera/ESGetEyeRotationEvent.cs

using Content.Shared.Camera;

namespace Content.Shared._ES.Camera;

/// <summary>
///     Raised directed by-ref on the local eye entity whenever its lerped rotation is applied.
///     Should be subscribed to by any systems that want to modify an entity's eye rotation,
///     so that they do not override each other.
/// </summary>
/// <remarks>
///     Counterpart of <see cref="GetEyeOffsetEvent"/>, but for rotation, to use for screenshake.
/// </remarks>
[ByRefEvent]
public record struct ESGetEyeRotationEvent(Angle Rotation);
