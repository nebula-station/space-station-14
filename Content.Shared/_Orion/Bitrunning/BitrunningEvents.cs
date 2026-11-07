// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Shared/_Orion/Bitrunning/BitrunningEvents.cs

using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Bitrunning;

public sealed partial class BitrunningDisconnectAvatarActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class BitrunningDisconnectAvatarDoAfterEvent : SimpleDoAfterEvent;
