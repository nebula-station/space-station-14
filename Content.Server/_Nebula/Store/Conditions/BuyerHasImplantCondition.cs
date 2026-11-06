// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Implants.Components;
using Content.Shared.Mind;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Server._Nebula.Store.Conditions;

/// <summary>
/// Only passes when the buyer's body carries one of <see cref="Implants"/>, buying the implanter is not enough.
/// </summary>
public sealed partial class BuyerHasImplantCondition : ListingCondition
{
    [DataField(required: true)]
    public HashSet<EntProtoId> Implants = [];

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;
        var body = ent.TryGetComponent<MindComponent>(args.Buyer, out var mind) ? mind.OwnedEntity : args.Buyer;
        if (!ent.TryGetComponent<ImplantedComponent>(body, out var implanted))
            return false;

        foreach (var implant in implanted.ImplantContainer.ContainedEntities)
        {
            if (ent.GetComponent<MetaDataComponent>(implant).EntityPrototype is { } proto && Implants.Contains(proto.ID))
                return true;
        }

        return false;
    }
}
