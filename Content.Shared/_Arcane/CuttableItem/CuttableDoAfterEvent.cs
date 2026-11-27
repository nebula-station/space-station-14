// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/6167a5fa73614d5347c461bb0fd8d7abe532ff44/Content.Shared/_Arcane/CuttableItem/CuttableDoAfterEvent.cs

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Arcane.CuttableItem;

[Serializable, NetSerializable]
public sealed partial class CuttableDoAfterEvent : SimpleDoAfterEvent { }
