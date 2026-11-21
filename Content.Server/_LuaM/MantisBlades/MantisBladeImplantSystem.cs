// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Server/_LuaM/MantisBlades/MantisBladeImplantSystem.cs

using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Server._LuaM.Cyberware;
using Content.Shared._LuaM.MantisBlades;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Server._LuaM.MantisBlades;

/// <summary>
/// Handles mantis blade arm implants: one action extends or retracts the blades of every arm at once.
/// </summary>
public sealed partial class MantisBladeImplantSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private BodyPartSystem _part = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    // The psycho pulls the blades out, but never puts them away.
    [SubscribeLocalEvent]
    private void OnPsychosisUse(Entity<MantisBladeImplantUserComponent> ent, ref CyberpsychosisUseImplantsEvent args)
    {
        foreach (var implant in _body.GetOrgans<MantisBladeImplantComponent>(ent.Owner))
        {
            if (implant.Comp.BladeUid is { } blade && !TerminatingOrDeleted(blade))
                return;
        }

        var toggle = new ToggleMantisBladesEvent();
        OnToggle(ent, ref toggle);
    }

    [SubscribeLocalEvent]
    private void OnInserted(Entity<MantisBladeImplantComponent> ent, ref OrganGotInsertedEvent args)
    {
        var user = EnsureComp<MantisBladeImplantUserComponent>(args.Target);
        if (user.ActionUid == null || TerminatingOrDeleted(user.ActionUid.Value))
            user.ActionUid = _actions.AddAction(args.Target, ent.Comp.ActionProto);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<MantisBladeImplantComponent> ent, ref OrganGotRemovedEvent args)
    {
        RetractBlade(ent);

        var body = args.Target.Owner;
        if (TerminatingOrDeleted(body))
            return;

        // Keep the action while another arm still has a blade.
        foreach (var other in _body.GetOrgans<MantisBladeImplantComponent>(body))
        {
            if (other.Owner != ent.Owner)
                return;
        }

        RemComp<MantisBladeImplantUserComponent>(body);
    }

    [SubscribeLocalEvent]
    private void OnImplantShutdown(Entity<MantisBladeImplantComponent> ent, ref ComponentShutdown args)
    {
        RetractBlade(ent);
    }

    // An EMP disables the implant through its Cybernetics component, the blade goes back in.
    [SubscribeLocalEvent]
    private void OnDisabled(Entity<MantisBladeImplantComponent> ent, ref OrganDisabledEvent args)
    {
        RetractBlade(ent);
    }

    [SubscribeLocalEvent]
    private void OnUserShutdown(Entity<MantisBladeImplantUserComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionUid);
    }

    [SubscribeLocalEvent]
    private void OnToggle(Entity<MantisBladeImplantUserComponent> ent, ref ToggleMantisBladesEvent args)
    {
        var implants = _body.GetOrgans<MantisBladeImplantComponent>(ent.Owner);
        if (implants.Count == 0)
            return;

        // If any blade is out, retract them all.
        var anyExtended = false;
        foreach (var implant in implants)
        {
            if (implant.Comp.BladeUid is { } blade && !TerminatingOrDeleted(blade))
                anyExtended = true;
            else
                implant.Comp.BladeUid = null;
        }

        if (anyExtended)
        {
            foreach (var implant in implants)
            {
                RetractBlade(implant);
            }

            _audio.PlayPvs(implants[0].Comp.RetractSound, ent);
            args.Handled = true;
            return;
        }

        var extended = false;
        var disabled = false;
        foreach (var implant in implants)
        {
            if (!HasComp<EnabledOrganComponent>(implant))
            {
                disabled = true;
                continue;
            }

            if (GetHandLocation(implant) is not { } location
                || GetHand(ent.Owner, location) is not { } hand)
                continue;

            // The blade pushes out whatever is held in that hand.
            if (_hands.GetHeldItem(ent.Owner, hand) != null)
                _hands.TryDrop(ent.Owner, hand, checkActionBlocker: false);

            var blade = SpawnAtPosition(implant.Comp.BladeProto, Transform(ent).Coordinates);
            if (!_hands.TryPickup(ent.Owner, blade, hand))
            {
                Del(blade);
                continue;
            }

            EnsureComp<UnremoveableComponent>(blade);
            implant.Comp.BladeUid = blade;
            extended = true;
        }

        if (extended)
        {
            _audio.PlayPvs(implants[0].Comp.ExtendSound, ent);
            args.Handled = true;
        }
        else if (disabled)
        {
            _popup.PopupEntity(Loc.GetString("mantis-blade-disabled-emp"), ent, ent);
        }
    }

    private void RetractBlade(Entity<MantisBladeImplantComponent> ent)
    {
        if (ent.Comp.BladeUid is { } blade && !TerminatingOrDeleted(blade))
            QueueDel(blade);

        ent.Comp.BladeUid = null;
    }

    /// <summary>
    /// Picks the hand side matching the arm the implant sits in.
    /// </summary>
    private HandLocation? GetHandLocation(EntityUid implant)
    {
        if (_part.GetParentPart(implant) is not { } arm)
            return null;

        return _part.GetSymmetry(arm) switch
        {
            BodyPartSymmetry.Left => HandLocation.Left,
            BodyPartSymmetry.Right => HandLocation.Right,
            _ => HandLocation.Middle,
        };
    }

    private string? GetHand(EntityUid user, HandLocation location)
    {
        foreach (var hand in _hands.EnumerateHands(user))
        {
            if (_hands.TryGetHand(user, hand, out var handData) && handData.Value.Location == location)
                return hand;
        }

        return null;
    }
}
