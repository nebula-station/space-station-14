// SPDX-License-Identifier: AGPL-3.0-or-later

// Ported from Radiant Sector: https://github.com/TetrisZz/frontier-Radiant

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.Leash;

[Serializable, NetSerializable]
public sealed partial class LeashAttachDoAfterEvent : SimpleDoAfterEvent;
