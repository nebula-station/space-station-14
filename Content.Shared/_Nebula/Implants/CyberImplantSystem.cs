// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Shared.Actions.Components;
using Content.Shared.Forensics;
using Content.Shared.Forensics.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.RetractableItemAction;
using Robust.Shared.Containers;

namespace Content.Shared._Nebula.Implants;

/// <summary>
/// Glue for implants that put upstream components on the body.
/// </summary>
public sealed partial class CyberImplantSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    [SubscribeLocalEvent]
    private void OnTryAccessFingerprint(Entity<FingerprintMaskComponent> ent, ref TryAccessFingerprintEvent args)
    {
        if (args.Cancelled)
            return;

        args.Blocker = ent.Owner;
        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnOrganDisabled(Entity<RetractOnOrganDisabledComponent> ent, ref OrganDisabledEvent args)
    {
        if (!TryComp<ActionsContainerComponent>(ent, out var actions))
            return;

        foreach (var action in actions.Container.ContainedEntities)
        {
            if (!TryComp<RetractableItemActionComponent>(action, out var retractable)
                || retractable.ActionItemUid is not { } item
                || !_hands.IsHolding(args.Body, item)
                || !_container.TryGetContainer(action, RetractableItemActionComponent.ContainerId, out var container))
                continue;

            RemComp<UnremoveableComponent>(item);
            _container.Insert(item, container);
        }
    }
}
