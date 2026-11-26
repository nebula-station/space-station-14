// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/02b6d617ada28d2a78cfd227b9860838fb082909/Content.Arcane.Server/Research/Systems/ResearchDataDiskSystem.cs

using System.Linq;
using Content.Server._Arcane.Research.Components;
using Content.Server.Popups;
using Content.Server.Research.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Research.Components;

namespace Content.Server._Arcane.Research.Systems;

public sealed partial class ResearchDataDiskSystem : EntitySystem
{
    [Dependency] private PopupSystem _popupSystem = default!;
    [Dependency] private ResearchSystem _research = default!;

    public override void Initialize()
    {
    }

    [SubscribeLocalEvent]
    private void OnAfterInteract(EntityUid uid, ResearchDataDiskComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;

        // Any server of a network reads and writes the network's database, which lives on its authority server.
        if (args.Target is not { } target || !_research.TryGetNetworkDatabase(target, out var network))
            return;

        if (component.HasDataSnapshot)
        {
            var imported = ImportDiskData(component, network);
            _popupSystem.PopupEntity(Loc.GetString("research-disk-data-imported", ("count", imported)), target, args.User);
            _research.LogNetworkEvent(network, "disk", Loc.GetString("research-netlog-disk-imported", ("count", imported)), args.User);
            args.Handled = true;
            return;
        }

        ExportDiskData(uid, component, network);
        _popupSystem.PopupEntity(Loc.GetString("research-disk-exported", ("count", component.StoredTechnologies.Count)), target, args.User);
        _research.LogNetworkEvent(network, "disk", Loc.GetString("research-netlog-disk-exported", ("count", component.StoredTechnologies.Count)), args.User);
        args.Handled = true;
    }

    private void ExportDiskData(EntityUid diskUid, ResearchDataDiskComponent disk, Entity<ResearchServerComponent, TechnologyDatabaseComponent> network)
    {
        disk.HasDataSnapshot = true;
        disk.StoredTechnologies = network.Comp2.ResearchedTechnologies.Select(x => x.ToString()).ToList();
        disk.StoredRecipes = network.Comp2.IndependentRecipes.Select(x => x.ToString()).ToList();
        disk.SnapshotServerName = network.Comp1.ServerName;
        Dirty(diskUid, disk);
    }

    private int ImportDiskData(ResearchDataDiskComponent disk, Entity<ResearchServerComponent, TechnologyDatabaseComponent> network)
    {
        return _research.ImportTechnologySnapshot(network, disk.StoredTechnologies, disk.StoredRecipes, network.Comp2);
    }

    [SubscribeLocalEvent]
    private void OnExamined(EntityUid uid, ResearchDataDiskComponent component, ExaminedEvent args)
    {
        if (!component.HasDataSnapshot)
        {
            args.PushMarkup(Loc.GetString("research-disk-data-empty"));
            return;
        }

        args.PushMarkup(Loc.GetString("research-disk-data-examine",
            ("server", string.IsNullOrWhiteSpace(component.SnapshotServerName) ? Loc.GetString("research-disk-data-unknown-server") : component.SnapshotServerName),
            ("count", component.StoredTechnologies.Count)));
    }
}
