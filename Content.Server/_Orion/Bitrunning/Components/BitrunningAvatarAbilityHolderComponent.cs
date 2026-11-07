// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Server/_Orion/Bitrunning/Components/BitrunningAvatarAbilityHolderComponent.cs

namespace Content.Server._Orion.Bitrunning.Components;

/// <summary>
/// Tracks actions granted to an avatar by bitrunning disks currently in its inventory tree.
/// </summary>
[RegisterComponent]
public sealed partial class BitrunningAvatarAbilityHolderComponent : Component
{
    public Dictionary<EntityUid, EntityUid?> ActionsByDisk = [];
}
