// SPDX-License-Identifier: MIT

// Ported from https://github.com/funky-station/forky-station/blob/500074aca67df6fc7fe86b9977a16e35f9ba533f/Content.Shared/_Starfall/Offering/SharedOfferingSystem.cs

using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Strip.Components;

namespace Content.Shared._Starfall.Offering;

/// <summary>
/// Handles consensual hand-to-hand item transfers.
/// </summary>
public sealed partial class SharedOfferingSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnStripHandInsertAttempt(ref StripHandInsertAttemptEvent args)
    {
        if (!CanOffer(args.User, args.Recipient, args.Item))
            return;

        var offered = EnsureComp<OfferedItemComponent>(args.Item);
        offered.Offerer = args.User;
        offered.Recipient = args.Recipient;
        offered.DoAfterIndex = args.DoAfterIndex;
        Dirty(args.Item, offered);
    }

    [SubscribeLocalEvent]
    private void OnStripDoAfter(Entity<DoAfterComponent> user, ref StrippableDoAfterEvent args)
    {
        // The offer lives only while its insert do-after runs, so it ends on both completion and cancellation.
        if (!args.InsertOrRemove
            || args.InventoryOrHand
            || args.Used is not { } item
            || !TryComp<OfferedItemComponent>(item, out var offered)
            || offered.Offerer != user.Owner
            || offered.DoAfterIndex != args.DoAfter.Index)
            return;

        RemComp(item, offered);
    }

    [SubscribeLocalEvent(before: [typeof(InteractionPopupSystem)])]
    private void OnInteractHand(EntityUid offerer, StrippableComponent component, InteractHandEvent args)
    {
        if (args.Handled
            || args.User == offerer
            || !TryComp<HandsComponent>(args.User, out var recipientHands)
            || _hands.GetActiveItem((args.User, recipientHands)) != null
            || !TryComp<HandsComponent>(offerer, out var offererHands))
            return;

        foreach (var handName in offererHands.Hands.Keys)
        {
            if (!_hands.TryGetHeldItem((offerer, offererHands), handName, out var item)
                || !TryComp<OfferedItemComponent>(item.Value, out var offered)
                || offered.Offerer != offerer
                || offered.Recipient != args.User)
                continue;

            args.Handled = TryAccept(args.User, offerer, item.Value, handName);
            return;
        }
    }

    [SubscribeLocalEvent]
    private void OnStripHandRemoveAttempt(Entity<OfferedItemComponent> item, ref StripHandRemoveAttemptEvent args)
    {
        if (item.Comp.Offerer != args.Holder || item.Comp.Recipient != args.User)
            return;

        TryAccept(args.User, args.Holder, item.Owner, args.HandName);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnParentChanged(Entity<OfferedItemComponent> item, ref EntParentChangedMessage args)
    {
        // An offer is only valid while the original offerer continuously holds the item.
        if (TerminatingOrDeleted(item.Comp.Offerer)
            || !TryComp<HandsComponent>(item.Comp.Offerer, out var hands)
            || !_hands.IsHolding((item.Comp.Offerer, hands), item.Owner))
            RemCompDeferred<OfferedItemComponent>(item);
    }

    private bool CanOffer(EntityUid offerer, EntityUid recipient, EntityUid item)
    {
        return offerer != recipient && item != offerer && item != recipient
            && !TerminatingOrDeleted(offerer) && !TerminatingOrDeleted(recipient) && !TerminatingOrDeleted(item)
            && TryComp<HandsComponent>(offerer, out var offererHands)
            && HasComp<HandsComponent>(recipient)
            && _hands.GetActiveItem((offerer, offererHands)) == item;
    }

    private bool TryAccept(EntityUid recipient, EntityUid offerer, EntityUid item, string handName)
    {
        // The offer must still be active, meant for this recipient, and the offerer must still hold the item in that hand.
        if (recipient == offerer || item == recipient || item == offerer
            || TerminatingOrDeleted(recipient) || TerminatingOrDeleted(offerer) || TerminatingOrDeleted(item)
            || !TryComp<OfferedItemComponent>(item, out var offered)
            || offered.Recipient != recipient || offered.Offerer != offerer
            || !TryComp<HandsComponent>(offerer, out var offererHands)
            || _hands.GetHeldItem((offerer, offererHands), handName) != item)
        {
            PopupFailure(recipient);
            return false;
        }

        // Remove the offer immediately so duplicate events in this tick cannot accept it twice.
        RemComp<OfferedItemComponent>(item);
        if (!_hands.TryDrop((offerer, offererHands), item, checkActionBlocker: false))
        {
            PopupFailure(recipient);
            return false;
        }

        if (!_hands.TryPickupAnyHand(recipient, item))
        {
            // Pickup hooks can still reject the transfer after the initial validation. Put it back if possible.
            _hands.TryPickup(offerer, item, handName, checkActionBlocker: false, handsComp: offererHands);
            PopupFailure(recipient);
            return false;
        }

        _popup.PopupEntity(Loc.GetString("offering-system-accepted-self", ("item", item)), recipient, recipient);
        return true;
    }

    private void PopupFailure(EntityUid recipient)
    {
        if (!TerminatingOrDeleted(recipient))
            _popup.PopupEntity(Loc.GetString("offering-system-cannot-accept"), recipient, recipient);
    }
}
