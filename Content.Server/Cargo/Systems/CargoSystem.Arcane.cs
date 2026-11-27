// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/accb2613b4ea35df58380cc8c7c82cfb2968670a/Content.Server/Cargo/Systems/CargoSystem.Arcane.cs

using System.Linq;
using Content.Shared._Arcane.CCVar;
using Content.Shared._Arcane.Economy;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Item;
using Content.Shared.Storage.Components;
using Robust.Shared.Map;

namespace Content.Server.Cargo.Systems;

/// <summary>
/// Arcane - requester names from IDs, delivery destinations and secured department crates (Orion #377).
/// </summary>
public sealed partial class CargoSystem
{
    [Dependency] private EntityStorageSystem _entityStorage = default!;

    private const int MaxOrderTextLength = 64;

    /// <summary>
    /// The orderer's ID name, or the typed one on an emagged console.
    /// </summary>
    private string GetRequester(Entity<CargoOrderConsoleComponent> console, CargoConsoleAddOrderMessage args)
    {
        var requester = console.Comp.EditableRequesterName && !string.IsNullOrWhiteSpace(args.Requester)
            ? Truncate(args.Requester)
            : GenerateRequesterName(args.Actor);

        if (!args.PayPrivately || console.Comp.Mode != CargoOrderConsoleMode.DirectOrder)
            return requester;

        var lookup = new BankAccountLookupEvent(args.Actor);
        RaiseLocalEvent(ref lookup);
        return lookup.AccountId == null
            ? requester
            : Loc.GetString("cargo-console-private-requester", ("requester", requester));
    }

    /// <summary>
    /// Copies the destination and secured delivery choice of a new order, dropping secured delivery when not allowed.
    /// </summary>
    private void SetOrderExtras(CargoOrderData data, Entity<CargoOrderConsoleComponent> console, CargoConsoleAddOrderMessage args, CargoProductPrototype product)
    {
        data.DeliveryDestination = string.IsNullOrWhiteSpace(args.DeliveryDestination) ? null : Truncate(args.DeliveryDestination);
        data.SecuredDelivery = args.SecuredDelivery
            && ProtoMan.Resolve(product.Product, out var productProto)
            && CanBeSecuredDelivery(console, productProto);
        data.ExtraCost = data.SecuredDelivery ? console.Comp.SecureOrderCost : 0;

        if (!args.PayPrivately || console.Comp.Mode != CargoOrderConsoleMode.DirectOrder)
            return;

        var lookup = new BankAccountLookupEvent(args.Actor);
        RaiseLocalEvent(ref lookup);
        if (lookup.AccountId == null)
        {
            _popup.PopupCursor(Loc.GetString("cargo-console-no-linked-bank-account"), args.Actor);
            return;
        }

        data.PrivateAccountId = lookup.AccountId;
    }

    /// <summary>
    /// What the buyer pays for a personal order, null for department orders.
    /// </summary>
    private int? GetPrivateOrderCost(CargoOrderData order, int cost)
    {
        if (order.PrivateAccountId == null)
            return null;

        return (int) Math.Round(cost * _cfg.GetCVar(ArcaneCVars.PrivateOrderMarkup), MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Moves credits on the personal account paying for an order, negative amounts charge it.
    /// </summary>
    private bool ChangePrivateAccount(CargoOrderData order, int amount)
    {
        if (order.PrivateAccountId is not { } account)
            return false;

        var ev = new BankAccountChangeEvent(account, amount, $"cargo order {order.OrderId}");
        RaiseLocalEvent(ref ev);
        return ev.Handled;
    }

    /// <summary>
    /// Puts an item, or the contents of a crate, into the account's secure crate.
    /// </summary>
    private EntityUid PackSecureDelivery(CargoOrderData order, CargoAccountPrototype account, EntityUid item, EntityCoordinates spawn)
    {
        if (!order.SecuredDelivery || account.SecureCratePrototype is not { } crateProto)
            return item;

        var crate = SpawnAtPosition(crateProto, spawn);

        if (TryComp<EntityStorageComponent>(item, out var storage))
        {
            foreach (var content in storage.Contents.ContainedEntities.ToArray())
            {
                if (!_entityStorage.Insert(content, crate))
                    return item; // crate is full, keep the rest in the original
            }

            QueueDel(item);
            return crate;
        }

        if (HasComp<ItemComponent>(item) && _entityStorage.Insert(item, crate))
            return crate;

        QueueDel(crate);
        return item;
    }

    private static string Truncate(string text)
    {
        text = text.Trim();
        return text.Length > MaxOrderTextLength ? text[..MaxOrderTextLength] : text;
    }
}
