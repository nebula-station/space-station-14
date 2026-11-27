// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/6167a5fa73614d5347c461bb0fd8d7abe532ff44/Content.Server/_Arcane/CuttableItem/Systems/CuttableItemSystem.cs

using Content.Server.Radio.EntitySystems;
using Content.Shared.Radio;
using Content.Shared._Arcane.CuttableItem.Components;
using Robust.Shared.Prototypes;
using Content.Shared._Arcane.CuttableItem;
using Content.Shared.Popups;
using Content.Shared.Inventory;

namespace Content.Server._Arcane.CuttableItem.Systems;

public sealed partial class CuttableItemSystem : EntitySystem
{
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;

    [SubscribeLocalEvent]
    private void OnCutCompleted(EntityUid uid, CuttableItemComponent comp, CuttableDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var victim = Transform(uid).ParentUid;
        if (!victim.IsValid())
            return;

        if (!_inventorySystem.TryGetContainingSlot(uid, out var slotDef))
            return;

        var target = args.User;

        if (!_inventorySystem.TryUnequip(target, victim, slotDef.Name, force: true))
            return;

        _transformSystem.AttachToGridOrMap(uid);

        var victimCoords = Transform(victim).Coordinates;
        _transformSystem.SetCoordinates(uid, victimCoords);

        _popup.PopupEntity(Loc.GetString("cuttable-item-broken-moment-popup", ("item", uid)), uid);

        var ev = new CuttableCutEvent(target);
        RaiseLocalEvent(uid, ev);
    }

    [SubscribeLocalEvent]
    private void OnItemCut(EntityUid uid, CuttableItemComponent comp, CuttableCutEvent args)
    {
        if (!_prototypeManager.TryIndex(comp.RadioChannel, out var channel))
            return;

        var userName = Name(args.User);
        var userItem = Name(uid);

        var message = Loc.GetString(comp.AlertMessage, ("user", userName), ("item", userItem));

        _radio.SendRadioMessage(uid, message, channel, uid);
    }
}
