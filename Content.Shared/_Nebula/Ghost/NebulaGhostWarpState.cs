// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.Ghost;

/// <summary>
/// Whether a player warp is played, left without a player or dead.
/// </summary>
[Serializable, NetSerializable]
public enum NebulaGhostWarpState : byte
{
    Active,
    Ssd,
    Dead,
}
