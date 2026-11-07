// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Server/_Orion/Bitrunning/Components/AvatarNavRelayComponent.cs

using Content.Server._Orion.Bitrunning.Systems;

namespace Content.Server._Orion.Bitrunning.Components;

[RegisterComponent]
[Access(typeof(QuantumServerSystem), typeof(NetpodSystem))]
public sealed partial class AvatarNavRelayComponent : Component
{
    /// <summary>
    /// Paired relay entity used for camera/view subscription redirection between avatar and netpod.
    /// Set when an avatar is spawned/connected, and on disconnect active viewers are removed before this link is cleared.
    /// If null, callers should publish ActiveCamera = null instead of falling back to a stale netpod target.
    /// </summary>
    public EntityUid? RelayEntity;
}
