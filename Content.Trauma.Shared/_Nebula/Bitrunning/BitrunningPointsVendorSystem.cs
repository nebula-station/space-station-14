// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared._Orion.Bitrunning.Systems;
using Content.Trauma.Shared.VendingMachines;

namespace Content.Trauma.Shared._Nebula.Bitrunning;

/// <summary>
/// Pays for <see cref="ShopVendorComponent"/> purchases with bitrunning points.
/// </summary>
public sealed partial class BitrunningPointsVendorSystem : EntitySystem
{
    [Dependency] private BitrunningPointsSystem _points = default!;

    [SubscribeLocalEvent]
    private void OnBalance(Entity<BitrunningPointsVendorComponent> ent, ref ShopVendorBalanceEvent args)
    {
        args.Balance = _points.GetPointComp(args.User)?.Comp?.Points ?? 0;
    }

    [SubscribeLocalEvent]
    private void OnPurchase(Entity<BitrunningPointsVendorComponent> ent, ref ShopVendorPurchaseEvent args)
    {
        if (args.Paid)
            return;

        if (_points.GetPointComp(args.User) is { } account && _points.RemovePoints(account, args.Cost))
            args.Paid = true;
    }
}
