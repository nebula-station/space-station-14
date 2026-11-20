// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._Nebula.Implants;

/// <summary>
/// Locks the item in the hand of an <see cref="AntiDropImplantComponent"/> so it can't be dropped, knocked out or taken.
/// </summary>
public sealed partial class AntiDropImplantSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnToggle(Entity<AntiDropImplantComponent> ent, ref ToggleAntiDropEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (ent.Comp.Locked != null)
        {
            Release(ent);
            _popup.PopupClient(Loc.GetString("anti-drop-popup-release"), args.Performer, args.Performer);
            return;
        }

        if (!TryLock(ent, args.Performer))
            return;

        ent.Comp.Action = args.Action.Owner;
        Dirty(ent);
        _actions.SetToggled(args.Action.AsNullable(), true);
        _popup.PopupClient(Loc.GetString("anti-drop-popup-lock"), args.Performer, args.Performer);
    }

    [SubscribeLocalEvent]
    private void OnDisabled(Entity<AntiDropImplantComponent> ent, ref OrganDisabledEvent args)
    {
        Release(ent);
    }

    [SubscribeLocalEvent]
    private void OnLockedUnequipped(Entity<AntiDropLockedComponent> ent, ref GotUnequippedHandEvent args)
    {
        ReleaseFromItem(ent);
    }

    // Other systems (glue, sticky, forced items) remove UnremoveableComponent on their own, so the lock can't rely on it.
    [SubscribeLocalEvent]
    private void OnLockedRemoveAttempt(Entity<AntiDropLockedComponent> ent, ref ContainerGettingRemovedAttemptEvent args)
    {
        if (_timing.ApplyingState || IsHandDetached(ent.Comp.Implant, args.Container.Owner))
            return;

        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnLockedShutdown(Entity<AntiDropLockedComponent> ent, ref ComponentShutdown args)
    {
        ReleaseFromItem(ent);
    }

    /// <summary>
    /// Locks the item in the implanted hand if it is the active one.
    /// </summary>
    public bool TryLock(Entity<AntiDropImplantComponent> ent, EntityUid body)
    {
        if (GetImplantHand(ent) is not { } hand
            || _hands.GetActiveHand(body) != hand)
        {
            _popup.PopupClient(Loc.GetString("anti-drop-popup-wrong-hand"), body, body);
            return false;
        }

        if (_hands.GetHeldItem(body, hand) is not { } item)
        {
            _popup.PopupClient(Loc.GetString("anti-drop-popup-empty"), body, body);
            return false;
        }

        var locked = EnsureComp<AntiDropLockedComponent>(item);
        locked.Implant = ent.Owner;
        locked.AddedUnremoveable = !HasComp<UnremoveableComponent>(item);
        if (locked.AddedUnremoveable)
        {
            var unremoveable = AddComp<UnremoveableComponent>(item);
            unremoveable.DeleteOnDrop = false;
            Dirty(item, unremoveable);
        }

        Dirty(item, locked);
        ent.Comp.Locked = item;
        Dirty(ent);
        return true;
    }

    public void Release(Entity<AntiDropImplantComponent> ent)
    {
        if (ent.Comp.Locked is not { } item)
            return;

        ent.Comp.Locked = null;
        Dirty(ent);
        if (ent.Comp.Action is { } action)
            _actions.SetToggled(action, false);

        if (TerminatingOrDeleted(item)
            || !TryComp<AntiDropLockedComponent>(item, out var locked)
            || locked.LifeStage > ComponentLifeStage.Running)
            return;

        if (locked.AddedUnremoveable)
            RemComp<UnremoveableComponent>(item);

        RemComp(item, locked);
    }

    private void ReleaseFromItem(Entity<AntiDropLockedComponent> ent)
    {
        if (TryComp<AntiDropImplantComponent>(ent.Comp.Implant, out var implant) && implant.Locked == ent.Owner)
            Release((ent.Comp.Implant, implant));
    }

    /// <summary>
    /// The implant's hand is no longer in the body, so the hand container is being shut down and a cancelled drop would delete the item.
    /// </summary>
    private bool IsHandDetached(EntityUid implant, EntityUid body)
    {
        return !TryComp<ChildOrganComponent>(implant, out var child)
            || child.Parent is not { } hand
            || !_container.TryGetContainingContainer(hand, out var container)
            || container.ID != BodyComponent.ContainerID
            || container.Owner != body;
    }

    private string? GetImplantHand(EntityUid implant)
    {
        return TryComp<ChildOrganComponent>(implant, out var child)
            && TryComp<HandOrganComponent>(child.Parent, out var hand)
                ? hand.HandID
                : null;
    }
}
