// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/93847a07a4b0b5890003c074e4768c5ba828543a/Content.Arcane.Shared/Medical/Concussion/ConcussionState.cs

using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Robust.Shared.Serialization;

namespace Content.Shared._Arcane.Medical.Concussion;

[Serializable, NetSerializable]
public enum ConcussionState : byte
{
    Invalid,
    Sane,
    Minor,
    Hard,
    Overwhelmed,
}

[ByRefEvent]
public readonly record struct ConcussionStateChangedEvent(ConcussionState OldState, ConcussionState NewState);

[ByRefEvent]
public record struct BeforeConcussionDamageEvent(FixedPoint2 Damage, bool Cancelled = false) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.HEAD | SlotFlags.EARS;
}
