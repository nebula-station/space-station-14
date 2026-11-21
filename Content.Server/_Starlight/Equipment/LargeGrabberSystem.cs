// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/TetrisZz/frontier-Radiant/blob/743dfcac03/Content.Server/_Starlight/Equipment/LargeGrabberSystem.cs

using Content.Server.Interaction;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.PowerCell;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server._Starlight.Equipment;

public sealed partial class LargeGrabberSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private InteractionSystem _interaction = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private TransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<LargeGrabberComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.ItemContainer = _container.EnsureContainer<Container>(ent.Owner, LargeGrabberComponent.ContainerId);
    }

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<LargeGrabberComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (_toggle.IsActivated(ent.Owner))
        {
            if (ent.Comp.ItemContainer.ContainedEntities.Count > 0)
                RemoveItem(ent, args.User, ent.Comp.ItemContainer.ContainedEntities[0]);

            UpdateState(ent);
            args.Handled = true;
            return;
        }

        if (args.Target is not { } target
            || target == args.User
            || ent.Comp.DoAfter != null
            || ent.Comp.ItemContainer.Count >= ent.Comp.MaxContents
            || TryComp<PhysicsComponent>(target, out var physics) && physics.BodyType == BodyType.Static
            || Transform(target).Anchored
            || !_interaction.InRangeUnobstructed(args.User, target)
            || _whitelist.IsWhitelistPass(ent.Comp.Blacklist, target)
            || !_powerCell.TryUseCharge(ent.Owner, ent.Comp.GrabEnergyCost, user: args.User))
            return;

        args.Handled = true;
        ent.Comp.AudioStream = _audio.PlayPvs(ent.Comp.GrabSound, ent.Owner)?.Entity;
        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.GrabDelay, new GrabberDoAfterEvent(), ent.Owner, target: target, used: ent.Owner)
        {
            BreakOnMove = true,
        };
        _doAfter.TryStartDoAfter(doAfter, out ent.Comp.DoAfter);
    }

    [SubscribeLocalEvent]
    private void OnGrab(Entity<LargeGrabberComponent> ent, ref GrabberDoAfterEvent args)
    {
        ent.Comp.DoAfter = null;
        if (args.Cancelled)
        {
            ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);
            return;
        }

        if (args.Handled || args.Args.Target is not { } target)
            return;

        _container.Insert(target, ent.Comp.ItemContainer);
        UpdateState(ent);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnRemovedFromContainer(Entity<LargeGrabberComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (!ent.Comp.DropOnContainerChange || TerminatingOrDeleted(ent))
            return;

        while (ent.Comp.ItemContainer.ContainedEntities.Count > 0)
        {
            var item = ent.Comp.ItemContainer.ContainedEntities[0];
            if (TerminatingOrDeleted(item))
                break;

            RemoveItem(ent, args.Container.Owner, item);
        }

        UpdateState(ent);
    }

    private void RemoveItem(Entity<LargeGrabberComponent> ent, EntityUid user, EntityUid item)
    {
        if (TerminatingOrDeleted(user) || TerminatingOrDeleted(item))
            return;

        _container.Remove(item, ent.Comp.ItemContainer);
        _transform.AttachToGridOrMap(item);
        var (position, rotation) = _transform.GetWorldPositionRotation(user);
        _transform.SetWorldPositionRotation(item, position + rotation.RotateVec(ent.Comp.DepositOffset), Angle.Zero);
    }

    private void UpdateState(Entity<LargeGrabberComponent> ent)
    {
        if (ent.Comp.ItemContainer.Count == 0)
            _toggle.TryDeactivate(ent.Owner);
        else if (ent.Comp.ItemContainer.Count >= ent.Comp.MaxContents)
            _toggle.TryActivate(ent.Owner);
    }
}
