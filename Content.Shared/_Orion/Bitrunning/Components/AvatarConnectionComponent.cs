// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Shared/_Orion/Bitrunning/Components/AvatarConnectionComponent.cs

using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.Bitrunning.Components;

[RegisterComponent, AutoGenerateComponentPause] // Nebula edit
public sealed partial class AvatarConnectionComponent : Component
{
    [DataField]
    public EntityUid? OriginalBody;

    [DataField]
    public EntityUid? Server;

    [DataField]
    public EntityUid? Netpod;

    [DataField]
    public EntityUid? RunnerMind;

    [DataField]
    public bool NoHit = true;

    [DataField]
    public bool DeleteOnDisconnect;

    [DataField]
    public EntProtoId DisconnectActionPrototype = "ActionBitrunningDisconnectAvatar";

    [DataField]
    public EntityUid? DisconnectActionEntity;

    [DataField, AutoPausedField] // Nebula edit
    public TimeSpan DisconnectBlockedUntil = TimeSpan.Zero;
}
