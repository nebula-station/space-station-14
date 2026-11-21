// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.Implants;

/// <summary>
/// Deploys and retracts the shield of an <see cref="ImplantShieldArmComponent"/>.
/// The shield only exists in its arm or in the hand of the arm's side.
/// </summary>
public sealed partial class ImplantShieldSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly ProtoId<OrganCategoryPrototype> ArmLeft = "ArmLeft";
    private static readonly ProtoId<OrganCategoryPrototype> HandLeft = "HandLeft";
    private static readonly ProtoId<OrganCategoryPrototype> HandRight = "HandRight";

    /// <summary>
    /// Shield being moved back by <see cref="Retract"/>, so its unequip isn't handled as a lost hand.
    /// </summary>
    private EntityUid? _retracting;

    #region Arm events

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<ImplantShieldArmComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<ContainerSlot>(ent, ImplantShieldArmComponent.ContainerId);
        if (_net.IsServer)
            SpawnShield(ent);
    }

    [SubscribeLocalEvent]
    private void OnToggle(Entity<ImplantShieldArmComponent> ent, ref ToggleImplantShieldEvent args)
    {
        if (args.Handled)
            return;

        // A deleted shield is replaced by the server, the client can't predict the spawn.
        if (ent.Comp.ShieldEntity is not { } shield)
        {
            if (_net.IsClient || SpawnShield(ent) is not { } spawned)
                return;

            shield = spawned;
        }

        args.Handled = true;
        ent.Comp.Action = args.Action.Owner;
        Dirty(ent);

        if (_hands.IsHolding(args.Performer, shield))
        {
            Retract(ent, shield);
            return;
        }

        TryDeploy(ent, shield, args.Performer);
    }

    [SubscribeLocalEvent]
    private void OnDisabled(Entity<ImplantShieldArmComponent> ent, ref OrganDisabledEvent args)
    {
        if (ent.Comp.ShieldEntity is { } shield)
            Retract(ent, shield);
    }

    #endregion

    #region Shield events

    // The shield's damage is applied by DamageableSystem in the same event.
    [SubscribeLocalEvent(after: [typeof(DamageableSystem)])]
    private void OnShieldDamaged(Entity<ImplantShieldComponent> ent, ref DamageDealtEvent args)
    {
        if (_damageable.GetTotalDamage(ent.Owner) < ent.Comp.BreakDamage
            || !TryComp<ImplantShieldArmComponent>(ent.Comp.Arm, out var arm))
            return;

        Retract((ent.Comp.Arm.Value, arm), ent.Owner);
        _damageable.ClearAllDamage(ent.Owner);

        if (arm.Action is { } action)
            _actions.SetCooldown(action, arm.BrokenCooldown);

        if (_body.GetBody(ent.Comp.Arm.Value) is { } body)
            _popup.PopupEntity(Loc.GetString("implant-shield-popup-broken"), body, body, PopupType.MediumCaution);
    }

    /// <summary>
    /// The shield left the hand some other way, e.g. the hand was cut off: put it back or remove it.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnShieldUnequipped(Entity<ImplantShieldComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (_net.IsClient
            || _retracting == ent.Owner
            || TerminatingOrDeleted(ent))
            return;

        if (ent.Comp.Arm is not { } arm
            || TerminatingOrDeleted(arm)
            || !_container.TryGetContainer(arm, ImplantShieldArmComponent.ContainerId, out var container))
        {
            QueueDel(ent);
            return;
        }

        if (!_container.IsEntityInContainer(ent))
            _container.Insert(ent.Owner, container);
    }

    [SubscribeLocalEvent]
    private void OnShieldShutdown(Entity<ImplantShieldComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Arm is not { } armUid
            || TerminatingOrDeleted(armUid)
            || !TryComp<ImplantShieldArmComponent>(armUid, out var arm)
            || arm.ShieldEntity != ent.Owner)
            return;

        arm.ShieldEntity = null;
        Dirty(armUid, arm);
    }

    #endregion

    #region Helpers

    private EntityUid? SpawnShield(Entity<ImplantShieldArmComponent> ent)
    {
        var container = _container.EnsureContainer<ContainerSlot>(ent, ImplantShieldArmComponent.ContainerId);
        var shield = Spawn(ent.Comp.Shield, MapCoordinates.Nullspace);
        var comp = EnsureComp<ImplantShieldComponent>(shield);
        comp.Arm = ent.Owner;
        Dirty(shield, comp);

        if (!_container.Insert(shield, container))
        {
            Log.Error($"Failed to insert {ToPrettyString(shield)} into {ToPrettyString(ent)}");
            Del(shield);
            return null;
        }

        ent.Comp.ShieldEntity = shield;
        Dirty(ent);
        return shield;
    }

    public bool TryDeploy(Entity<ImplantShieldArmComponent> ent, EntityUid shield, EntityUid body)
    {
        if (GetSideHand(ent) is not { } hand)
        {
            _popup.PopupClient(Loc.GetString("implant-shield-popup-no-hand"), body, body);
            return false;
        }

        if (_hands.GetHeldItem(body, hand) != null)
        {
            _popup.PopupClient(Loc.GetString("implant-shield-popup-hand-busy"), body, body);
            return false;
        }

        if (!_hands.TryPickup(body, shield, hand, checkActionBlocker: false, animate: false))
            return false;

        var unremoveable = EnsureComp<UnremoveableComponent>(shield);
        unremoveable.DeleteOnDrop = false;
        Dirty(shield, unremoveable);
        return true;
    }

    public void Retract(Entity<ImplantShieldArmComponent> ent, EntityUid shield)
    {
        if (!_container.TryGetContainer(ent, ImplantShieldArmComponent.ContainerId, out var container)
            || container.Contains(shield))
            return;

        RemComp<AntiDropLockedComponent>(shield);
        RemComp<UnremoveableComponent>(shield);
        _retracting = shield;
        try
        {
            _container.Insert(shield, container);
        }
        finally
        {
            _retracting = null;
        }
    }

    private string? GetSideHand(EntityUid arm)
    {
        if (!TryComp<OrganComponent>(arm, out var organ)
            || organ.Body is not { } body
            || !TryComp<ChildOrganComponent>(arm, out var child)
            || !TryComp<OrganComponent>(child.Parent, out var part))
            return null;

        var handCategory = part.Category == ArmLeft ? HandLeft : HandRight;
        return _body.GetOrgan(body, handCategory) is { } handOrgan
            && TryComp<HandOrganComponent>(handOrgan, out var hand)
                ? hand.HandID
                : null;
    }

    #endregion
}
