// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Shared/_Goobstation/Autosurgeon/AutoSurgeonSystem.cs

using System.Linq; // Nebula
using Content.Goobstation.Shared.Autosurgeon;
using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Shared._LuaM.Autosurgeon;
using Content.Shared._LuaM.Cyberware;
using Content.Shared._Nebula.Implants; // Nebula
using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server._LuaM.Autosurgeon;

/// <summary>
/// Installs Colossus cyberware from an autosurgeon and tells the user how much humanity they have left.
/// Runs before <see cref="AutoSurgeonSystem"/>, which marks the autosurgeon used.
/// </summary>
public sealed partial class CyberAutoSurgeonSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private BodyPartSystem _part = default!;
    [Dependency] private SharedHandsSystem _hands = default!; // Nebula edit
    [Dependency] private SharedSubdermalImplantSystem _implant = default!;
    [Dependency] private SharedPopupSystem _popup = default!; // Nebula edit

    [SubscribeLocalEvent(before: [typeof(AutoSurgeonSystem)])]
    private void OnDoAfter(Entity<CyberAutoSurgeonComponent> ent, ref AutoSurgeonDoAfterEvent args)
    {
        if (args.Cancelled
            || !TryComp<AutoSurgeonComponent>(ent, out var surgeon)
            || surgeon.Used
            || args.Target is not { } target
            || !HasComp<BodyComponent>(target))
            return;

        // Nebula start
        var part = GetTargetPart(ent.Comp, target);
        if (ent.Comp.ActiveHandPart != null && part == null)
        {
            _popup.PopupEntity(Loc.GetString("cyber-autosurgeon-popup-no-hand"), target, target, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }
        // Nebula end

        var coords = Transform(target).Coordinates;

        // Nebula start
        if (!CanInstall(target, ent.Comp.NewOrgan)
            || !CanInstall(target, ent.Comp.OrganForEachPart)
            || !CanInstall(target, ent.Comp.Implant)
            || ent.Comp.NewOrgans.Any(organ => !CanInstall(target, organ)))
        {
            args.Handled = true;
            return;
        }
        // Nebula end

        if (ent.Comp.NewOrgan is { } organProto
            && part != null) // Nebula edit
            TryInstall(part.Value, SpawnAtPosition(organProto, coords), replace: true);

        // Nebula start
        foreach (var organ in ent.Comp.NewOrgans)
        {
            if (_body.GetOrgan(target, ent.Comp.TargetPart) is { } newOrganPart)
                TryInstall(newOrganPart, SpawnAtPosition(organ, coords), replace: true);
        }
        // Nebula end

        if (ent.Comp.OrganForEachPart is { } eachProto)
        {
            foreach (var each in _part.GetBodyParts(target, ent.Comp.PartType))
            {
                TryInstall(each, SpawnAtPosition(eachProto, coords), replace: false);
            }
        }

        if (ent.Comp.Implant is { } implant)
            _implant.AddImplant(target, implant);

        if (ent.Comp.HumanityCost > 0
            && _body.GetOrgan(target, ent.Comp.TargetPart) is { } upgraded)
        {
            var key = ent.Comp.HumanityKey ?? MetaData(ent).EntityPrototype?.ID ?? string.Empty;
            EnsureComp<CyberwareHumanityComponent>(upgraded).Upgrades[key] = ent.Comp.HumanityCost;
        }

        var ev = new CyberwareInstalledEvent();
        RaiseLocalEvent(target, ref ev);
    }

    // Nebula start
    private bool CanInstall(EntityUid target, EntProtoId? cyberware)
    {
        if (cyberware is not { } id)
            return true;

        var ev = new CyberwareInstallAttemptEvent(id);
        RaiseLocalEvent(target, ref ev);
        if (ev.Cancelled && ev.Reason != null)
            _popup.PopupEntity(ev.Reason, target, target, PopupType.MediumCaution);

        return !ev.Cancelled;
    }

    private EntityUid? GetTargetPart(CyberAutoSurgeonComponent comp, EntityUid target)
    {
        if (comp.ActiveHandPart is not { } type)
            return _body.GetOrgan(target, comp.TargetPart);

        if (_hands.GetActiveHand(target) is not { } active)
            return null;

        foreach (var hand in _body.EnumerateOrgans<HandOrganComponent>(target))
        {
            if (hand.Comp2.HandID != active)
                continue;

            if (type == BodyPartType.Hand)
                return hand.Owner;

            return TryComp<ChildOrganComponent>(hand, out var child) ? child.Parent : null;
        }

        return null;
    }

    /// <summary>
    /// Moves the organs of a replaced part, like the hand of an arm, into the new part.
    /// </summary>
    private void MoveChildren(Entity<BodyPartComponent> part, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> children)
    {
        foreach (var (category, child) in children)
        {
            if (TerminatingOrDeleted(child))
                continue;

            if (!_part.HasOrganSlot(part.AsNullable(), category))
                _part.TryAddSlot(part.AsNullable(), category);

            if (!_part.InsertOrgan(part.AsNullable(), child))
                Log.Error($"{ToPrettyString(child)} could not be moved into {ToPrettyString(part)}");
        }
    }
    // Nebula end

    /// <summary>
    /// Puts the organ into the part, opening a slot for it if the part has none.
    /// The organ is deleted if it doesn't fit.
    /// </summary>
    private void TryInstall(EntityUid part, EntityUid organ, bool replace)
    {
        if (_body.GetCategory(organ) is not { } category
            || !TryComp<BodyPartComponent>(part, out var partComp))
        {
            Del(organ);
            return;
        }

        var parent = (part, partComp);
        if (!_part.HasOrganSlot(parent, category))
            _part.TryAddSlot(parent, category);

        // Nebula start
        Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid>? children = null;
        EntityUid? replaced = null;
        // Nebula end
        if (_part.GetOrgan(parent, category) is { } old)
        {
            if (!replace)
            {
                Del(organ);
                return;
            }

            // Nebula start
            if (TryComp<BodyPartComponent>(old, out var oldPart) && TryComp<BodyPartComponent>(organ, out _))
                children = new(oldPart.Children);
            // Nebula end
            _part.RemoveOrgan(parent, old);
            replaced = old; // Nebula edit
        }

        if (!_part.InsertOrgan(parent, organ))
        {
            Log.Error($"{ToPrettyString(organ)} could not be installed into {ToPrettyString(part)}");
            Del(organ);
            return; // Nebula edit
        }

        // Nebula start
        if (children == null || !TryComp<BodyPartComponent>(organ, out var newPart))
            return;

        MoveChildren((organ, newPart), children);
        Del(replaced);
        // Nebula end
    }
}
