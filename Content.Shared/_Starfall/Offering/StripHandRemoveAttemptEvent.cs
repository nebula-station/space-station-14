// SPDX-License-Identifier: MIT

// Ported from https://github.com/funky-station/forky-station/blob/500074aca67df6fc7fe86b9977a16e35f9ba533f/Content.Shared/_Starfall/Offering/StripHandRemoveAttemptEvent.cs

namespace Content.Shared._Starfall.Offering;

/// <summary>
/// Raised after the do-after for placing an active-hand item into a hand through the stripping menu has started.
/// </summary>
[ByRefEvent]
public readonly record struct StripHandInsertAttemptEvent(EntityUid User, EntityUid Recipient, EntityUid Item, string HandName, ushort DoAfterIndex);

/// <summary>
/// Raised before removing an item from a hand through the stripping menu.
/// </summary>
[ByRefEvent]
public record struct StripHandRemoveAttemptEvent(EntityUid User, EntityUid Holder, EntityUid Item, string HandName)
{
    public bool Handled;
}
