// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Augments;
using Content.Shared.Body;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared._Nebula.Implants;

public sealed partial class CyberToolArmSystem : EntitySystem
{
    [Dependency] private AugmentSystem _augment = default!;
    [Dependency] private AugmentToolPanelSystem _toolPanel = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedStorageSystem _storage = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CyberToolArmComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        foreach (var id in ent.Comp.Tools)
        {
            var tool = Spawn(id, MapCoordinates.Nullspace);
            var item = EnsureComp<CyberToolArmItemComponent>(tool);
            item.Arm = ent.Owner;
            Dirty(tool, item);

            if (!_storage.Insert(ent.Owner, tool, out _, playSound: false))
            {
                Log.Error($"Failed to insert {ToPrettyString(tool)} into {ToPrettyString(ent)}");
                Del(tool);
            }
        }
    }

    [SubscribeLocalEvent(before: [typeof(AugmentActivatableUISystem)])]
    private void OnAugmentAction(Entity<CyberToolArmComponent> ent, ref AugmentActionEvent args)
    {
        if (args.Handled
            || _augment.GetBody(ent) is not { } body
            || !TryComp<AugmentToolPanelComponent>(ent, out var panel))
            return;

        foreach (var held in _hands.EnumerateHeld(body))
        {
            if (!HasComp<AugmentToolPanelActiveItemComponent>(held)
                || !TryComp<CyberToolArmItemComponent>(held, out var item)
                || item.Arm != ent.Owner)
                continue;

            _toolPanel.SwitchTool((ent.Owner, panel), null, body);
            args.Handled = true;
            return;
        }
    }

    // The tools are only reachable through the radial, otherwise a loose arm would hand them out for free.
    [SubscribeLocalEvent]
    private void OnStorageInteractAttempt(Entity<CyberToolArmComponent> ent, ref StorageInteractAttemptEvent args)
    {
        args.Cancelled = true;
    }

    /// <summary>
    /// Puts a deployed tool back into its arm before the hand holding it is removed,
    /// otherwise the hand container is shut down with the undroppable tool inside and deletes it.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnOrganRemovedFrom(Entity<HandsComponent> ent, ref OrganRemovedFromEvent args)
    {
        if (_timing.ApplyingState
            || TerminatingOrDeleted(ent)
            || !TryComp<HandOrganComponent>(args.Organ, out var hand)
            || _hands.GetHeldItem(ent.AsNullable(), hand.HandID) is not { } tool
            || !HasComp<AugmentToolPanelActiveItemComponent>(tool)
            || !TryComp<CyberToolArmItemComponent>(tool, out var item)
            || item.Arm is not { } arm
            || TerminatingOrDeleted(arm))
            return;

        RemComp<AugmentToolPanelActiveItemComponent>(tool);
        if (!_storage.Insert(arm, tool, out _, playSound: false))
        {
            Log.Error($"Failed to retract {ToPrettyString(tool)} into {ToPrettyString(arm)}");
            EnsureComp<AugmentToolPanelActiveItemComponent>(tool);
            return;
        }

        _toggle.TryDeactivate(arm, user: ent.Owner);
    }
}
