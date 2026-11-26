// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/accb2613b4ea35df58380cc8c7c82cfb2968670a/Content.Arcane.Server/Economy/VendingPaymentSystem.cs

using Content.Shared._Arcane.Economy;
using Content.Server.Cargo.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Station.Systems;
using Content.Shared.VendingMachines;
using Content.Shared.VendingMachines.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Arcane.Economy;

/// <summary>
///     Charges the personal account of the buyer for vending machine items, the money goes to the station.
///     Cyborgs buy at the expense of the primary station account.
/// </summary>
public sealed partial class VendingPaymentSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private BankSystem _bank = default!;
    [Dependency] private CargoSystem _cargo = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private VendingPricingSystem _pricing = default!;

    [SubscribeLocalEvent]
    private void OnPurchaseAttempt(Entity<VendingMachineComponent> ent, ref VendingPurchaseAttemptEvent args)
    {
        if (args.User is not { } user)
            return;

        var price = _pricing.GetPrice(ent, user, args.ItemId, (InventoryType) args.InventoryType);
        if (price <= 0)
            return;

        if (_station.GetOwningStation(ent) is not { } station || !TryComp<StationBankAccountComponent>(station, out var bank))
        {
            Deny(ent, user, "vending-machine-purchase-payment-failed", ref args);
            return;
        }

        var target = _proto.TryIndex<VendingPricesPrototype>(ent.Comp.PackPrototypeId.Id, out var prices) && prices.Account is { } account
            ? account
            : bank.PrimaryAccount;

        if (HasComp<BorgChassisComponent>(user))
        {
            if (_cargo.GetBalanceFromAccount((station, bank), bank.PrimaryAccount) < price)
            {
                Deny(ent, user, "vending-machine-purchase-insufficient-funds", ref args);
                return;
            }

            _cargo.UpdateBankAccount((station, bank), -price, bank.PrimaryAccount);
            _cargo.UpdateBankAccount((station, bank), price, target);
            return;
        }

        if (!_bank.TryGetUserAccount(user, out var buyer))
        {
            Deny(ent, user, "vending-machine-purchase-no-id", ref args);
            return;
        }

        if (!_bank.ChangeBalance(buyer, -price, $"vending purchase of {args.ItemId}"))
        {
            Deny(ent, user, "vending-machine-purchase-insufficient-funds", ref args);
            return;
        }

        _cargo.UpdateBankAccount((station, bank), price, target);
    }

    private void Deny(EntityUid machine, EntityUid user, string message, ref VendingPurchaseAttemptEvent args)
    {
        _popup.PopupEntity(Loc.GetString(message), machine, user);
        args.Cancelled = true;
    }
}
