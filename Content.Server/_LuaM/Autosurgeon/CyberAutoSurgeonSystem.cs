// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Shared/_Goobstation/Autosurgeon/AutoSurgeonSystem.cs

using System.Linq;
using Content.Goobstation.Shared.Autosurgeon;
using Content.Medical.Shared.Body;
using Content.Shared._LuaM.Autosurgeon;
using Content.Shared._LuaM.Cyberware;
using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants;
using Content.Shared.Popups;
using Robust.Shared.Containers;
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
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSubdermalImplantSystem _implant = default!;
    [Dependency] private OrganRelationSystem _relation = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    [SubscribeLocalEvent(before: [typeof(AutoSurgeonSystem)])]
    private void OnDoAfter(Entity<CyberAutoSurgeonComponent> ent, ref AutoSurgeonDoAfterEvent args)
    {
        if (args.Cancelled
            || !TryComp<AutoSurgeonComponent>(ent, out var surgeon)
            || surgeon.Used
            || args.Target is not { } target
            || !HasComp<BodyComponent>(target))
            return;

        var coords = Transform(target).Coordinates;

        if (ent.Comp.NewOrgan is { } organProto)
        {
            var part = ent.Comp.ActiveSide
                ? GetActiveSidePart(target, organProto)
                : _body.GetOrgan(target, ent.Comp.TargetPart);

            if (part != null)
                TryInstall(part.Value, SpawnAtPosition(organProto, coords), replace: true);
            else if (ent.Comp.ActiveSide)
            {
                _popup.PopupEntity(Loc.GetString("cyber-autosurgeon-no-active-side"), target, target, PopupType.MediumCaution);
                args.Handled = true;
                return;
            }
        }

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

        var old = _part.GetOrgan(parent, category);
        if (old != null)
        {
            if (!replace)
            {
                Del(organ);
                return;
            }

            _part.RemoveOrgan(parent, old.Value);
        }

        if (!_part.InsertOrgan(parent, organ))
        {
            Log.Error($"{ToPrettyString(organ)} could not be installed into {ToPrettyString(part)}");
            Del(organ);
            return;
        }

        if (old != null)
            MoveChildren(old.Value, organ);
    }

    /// <summary>
    /// Moves the organs of a replaced body part into the new one, so a new arm keeps the hand and its implants.
    /// </summary>
    private void MoveChildren(EntityUid oldPart, EntityUid newPart)
    {
        if (!TryComp<BodyPartComponent>(oldPart, out var oldComp)
            || !TryComp<BodyPartComponent>(newPart, out var newComp)
            || _part.GetSeveredOrgansContainer((oldPart, oldComp)) is not { } severed)
            return;

        var parent = (newPart, newComp);
        foreach (var child in severed.ContainedEntities.ToList())
        {
            if (_body.GetCategory(child) is not { } category
                || _part.GetOrgan(parent, category) != null
                || !_container.Remove(child, severed))
                continue;

            if (!_part.HasOrganSlot(parent, category))
                _part.TryAddSlot(parent, category);

            if (!_part.InsertOrgan(parent, child))
                Log.Error($"{ToPrettyString(child)} could not be moved from {ToPrettyString(oldPart)} to {ToPrettyString(newPart)}");
        }
    }

    /// <summary>
    /// Finds the hand of the active hand or a part above it that the organ can be a child of.
    /// </summary>
    private EntityUid? GetActiveSidePart(EntityUid body, EntProtoId organ)
    {
        if (_hands.GetActiveHand(body) is not { } handId
            || !_proto.Index(organ).TryGetComponent<ChildOrganComponent>(out var child, Factory))
            return null;

        foreach (var hand in _body.GetOrgans<HandOrganComponent>(body))
        {
            if (hand.Comp.HandID != handId)
                continue;

            if (IsAllowedParent(hand, child))
                return hand;

            foreach (var parent in _relation.AllParents(hand.Owner))
            {
                if (IsAllowedParent(parent, child))
                    return parent;
            }

            return null;
        }

        return null;
    }

    private bool IsAllowedParent(EntityUid part, ChildOrganComponent child)
    {
        return _body.GetCategory(part) is { } category && child.Parents.Contains(category);
    }
}
