// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Store;
using Content.Shared.Whitelist;

namespace Content.Server._Nebula.Store.Conditions;

/// <summary>
/// Hides a listing from the stores matching <see cref="Blacklist"/>. Unlike <c>StoreWhitelistCondition</c> it passes
/// without a store entity, which keeps the listing in surplus bundles.
/// </summary>
public sealed partial class StoreBlacklistCondition : ListingCondition
{
    [DataField(required: true)]
    public EntityWhitelist Blacklist = new();

    public override bool Condition(ListingConditionArgs args)
    {
        return args.StoreEntity is not { } store
            || !args.EntityManager.System<EntityWhitelistSystem>().IsWhitelistPass(Blacklist, store);
    }
}
