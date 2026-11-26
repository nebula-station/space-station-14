// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/dbb6977519b7f974a2dd0e9ca1f4aca27ecc62c9/Content.Server/_Arcane/Store/Systems/StoreSystem.Refund.Arcane.cs

using Content.Shared.FixedPoint;
using Content.Shared._Arcane.Store.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Store.Systems;

public sealed partial class StoreSystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    private const float RefundRatio = 0.8f;

    private void ArcaneHandleUplinkRefundable(EntityUid storeUid, IReadOnlyDictionary<ProtoId<CurrencyPrototype>, FixedPoint2> cost, EntityUid product) // Nebula edit
    {
        if (!TryComp<StoreComponent>(storeUid, out _))
            return;

        var refundComp = EnsureComp<UplinkRefundableComponent>(product);
        refundComp.OriginalUplink = storeUid;

        // Nebula start
        foreach (var (currency, amount) in cost)
        {
            if (amount <= FixedPoint2.Zero)
                continue;

            refundComp.Currency = currency;
            refundComp.RefundAmount = FixedPoint2.New((int) MathF.Floor(amount.Float() * RefundRatio));
            break;
        }
        // Nebula end

        if (TryComp<ContainerManagerComponent>(product, out var containerManager))
        {
            List<string> prototypes = [];
            foreach (var container in containerManager.Containers.Values)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    var meta = MetaData(entity);
                    if (meta.EntityPrototype != null)
                        prototypes.Add(meta.EntityPrototype.ID);
                }
            }

            if (prototypes.Count > 0)
                refundComp.OriginalContainerPrototypes = prototypes;
        }

        if (TryComp<SolutionManagerComponent>(product, out var solutionManager))
        {
            Dictionary<string, FixedPoint2> reagents = [];
            foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((product, solutionManager)))
            {
                foreach (var (reagent, quantity) in soln.Comp.Solution.Contents)
                {
                    var id = reagent.Prototype.Id;
                    if (reagents.TryGetValue(id, out var existing))
                        reagents[id] = existing + quantity;
                    else
                        reagents[id] = quantity;
                }
            }

            refundComp.OriginalSolutionReagents = reagents;
        }
    }
}
