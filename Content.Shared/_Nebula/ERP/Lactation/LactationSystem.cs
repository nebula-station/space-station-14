// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Chemistry;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Containers;

namespace Content.Shared._Nebula.ERP.Lactation;

public sealed partial class LactationSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LactationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<LactationComponent, HarvestSolutionAttemptEvent>(OnHarvestAttempt);
    }

    private void OnMapInit(Entity<LactationComponent> ent, ref MapInitEvent args)
    {
        var manager = EnsureComp<SolutionManagerComponent>(ent);
        var container = _container.EnsureContainer<Container>(ent, manager.Container);
        foreach (var solution in container.ContainedEntities)
        {
            if (Prototype(solution)?.ID == ent.Comp.Solution.Id)
                return;
        }

        _solution.CreateSolution(ent.Comp.Solution, container);
    }

    private void OnHarvestAttempt(Entity<LactationComponent> ent, ref HarvestSolutionAttemptEvent args)
    {
        if (!IsCovered(ent))
            return;

        args.Cancelled = true;
        if (args.Popup)
            _popup.PopupClient(Loc.GetString("lactation-covered", ("target", Identity.Entity(ent, EntityManager))), ent, args.User);
    }

    private bool IsCovered(Entity<LactationComponent> ent)
    {
        foreach (var slot in ent.Comp.BlockingSlots)
        {
            if (_inventory.TryGetSlotEntity(ent, slot, out _))
                return true;
        }

        return false;
    }
}
