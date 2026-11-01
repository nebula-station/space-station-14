// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/93847a07a4b0b5890003c074e4768c5ba828543a/Content.Arcane.Common/Medical/Concussion/FlashbangConcussionEvent.cs

namespace Content.Shared._Arcane.Medical.Concussion;

/// <summary>
/// Carries the concussion dose after flashbang range and hearing protection have been resolved.
/// </summary>
[ByRefEvent]
public readonly record struct FlashbangConcussionEvent(float Damage);
