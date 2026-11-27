// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/02b6d617ada28d2a78cfd227b9860838fb082909/Content.Arcane.Server/Research/Systems/ExperimentalDestructiveScannerSystem.cs

using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Research.Systems;
using Content.Shared.Research;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Chat;
using Content.Shared.Item;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Arcane.Research.Systems;

public sealed partial class ExperimentalDestructiveScannerSystem : EntitySystem
{
    [Dependency] private ResearchSystem _research = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ChatSystem _chat = default!;

    public override void Initialize()
    {
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<ExperimentalDestructiveScannerComponent> ent, ref ComponentStartup args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);

        UpdateAppearance(ent, ExperimentalDestructiveScannerVisualState.Idle);
    }

    [SubscribeLocalEvent]
    private void OnUiOpened(Entity<ExperimentalDestructiveScannerComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnOpenServerMenu(Entity<ExperimentalDestructiveScannerComponent> ent, ref OpenResearchServerMenuMessage args)
    {
        _ui.TryToggleUi(ent.Owner, ResearchClientUiKey.Key, args.Actor);
    }

    [SubscribeLocalEvent]
    private void OnPointsChanged(Entity<ExperimentalDestructiveScannerComponent> ent, ref ResearchServerPointsChangedEvent args)
    {
        if (_ui.IsUiOpen(ent.Owner, ExperimentalDestructiveScannerUiKey.Key))
            UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnRegistrationChanged(Entity<ExperimentalDestructiveScannerComponent> ent, ref ResearchRegistrationChangedEvent args)
    {
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnPerform(Entity<ExperimentalDestructiveScannerComponent> ent, ref ExperimentalDestructiveScannerPerformMessage args)
    {
        if (ent.Comp.IsProcessing)
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-experimental-destructive-scanner-busy");
            _audio.PlayPvs(ent.Comp.FailureSound, ent, ent.Comp.AudioParams);
            UpdateUi(ent);
            return;
        }

        if (!_research.TryResolveMachineServer(ent, out var server))
        {
            ent.Comp.LastSubject = string.Empty;
            ent.Comp.LastResult = Loc.GetString("research-machine-common-no-server");
            _audio.PlayPvs(ent.Comp.FailureSound, ent, ent.Comp.AudioParams);
            UpdateUi(ent);
            return;
        }

        var xform = Transform(ent);
        List<EntityUid> items = [];

        if (xform.GridUid is { } gridUid &&
            TryComp(gridUid, out MapGridComponent? gridComp) &&
            _maps.TryGetTileRef(gridUid, gridComp, xform.Coordinates, out var tileRef))
        {
            items = _lookup.GetLocalEntitiesIntersecting(tileRef, 0f)
                .Where(uid => uid != ent.Owner
                              && HasComp<ItemComponent>(uid)
                              && !HasComp<ResearchClientComponent>(uid)
                              && !_container.TryGetContainingContainer(uid, out _))
                .Distinct()
                .ToList();
        }

        if (items.Count == 0)
        {
            ent.Comp.LastSubject = string.Empty;
            ent.Comp.LastResult = Loc.GetString("research-machine-experimental-destructive-scanner-no-items");
            _audio.PlayPvs(ent.Comp.FailureSound, ent, ent.Comp.AudioParams);
            UpdateUi(ent);
            return;
        }

        var itemContainer = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
        List<EntityUid> scannedItems = [];
        foreach (var item in items)
        {
            if (_container.Insert(item, itemContainer))
                scannedItems.Add(item);
        }

        if (scannedItems.Count == 0)
        {
            ent.Comp.LastSubject = string.Empty;
            ent.Comp.LastResult = Loc.GetString("research-machine-experimental-destructive-scanner-no-items");
            _audio.PlayPvs(ent.Comp.FailureSound, ent, ent.Comp.AudioParams);
            UpdateUi(ent);
            return;
        }

        var actor = args.Actor;

        ent.Comp.IsProcessing = true;
        UpdateAppearance(ent, ExperimentalDestructiveScannerVisualState.Down);
        ent.Comp.LastSubject = string.Join(", ", scannedItems.Select(uid => Name(uid)));
        ent.Comp.LastResult = Loc.GetString("research-machine-experimental-destructive-scanner-processing", ("count", scannedItems.Count));
        _research.LogNetworkEvent(server, "experimental-destructive-scanner", Loc.GetString("research-netlog-experimental-destructive-scanner-started", ("count", scannedItems.Count), ("user", _research.GetResearchLogUserName(args.Actor))), args.Actor);
        UpdateUi(ent);

        Timer.Spawn(ent.Comp.CapsuleStepDuration,
            () =>
            {
                if (TerminatingOrDeleted(ent) || !ent.Comp.IsProcessing)
                    return;

                UpdateAppearance(ent, ExperimentalDestructiveScannerVisualState.Scanning);
            });

        Timer.Spawn(ent.Comp.ScanDuration, () => CompleteScan(ent, server, scannedItems, actor));
    }

    private void CompleteScan(Entity<ExperimentalDestructiveScannerComponent> ent, EntityUid server, List<EntityUid> scannedItems, EntityUid? user)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var changedAny = false;
        HashSet<string> completedExperiments = [];

        foreach (var item in scannedItems)
        {
            if (TerminatingOrDeleted(item))
                continue;

            if (!_research.TryProgressExperimentsWithEntity(server, item, null, out var changed, out var completed, out _, source: ExperimentSourceFlags.MachineScanner))
                continue;

            changedAny |= changed;
            foreach (var completedExperiment in completed)
            {
                completedExperiments.Add(completedExperiment);
            }
        }

        var completedCount = completedExperiments.Count;

        ent.Comp.IsProcessing = false;
        UpdateAppearance(ent, ExperimentalDestructiveScannerVisualState.Up);

        Timer.Spawn(ent.Comp.CapsuleStepDuration,
            () =>
            {
                if (TerminatingOrDeleted(ent) || ent.Comp.IsProcessing)
                    return;

                var itemContainer = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
                _container.EmptyContainer(itemContainer, true, Transform(ent).Coordinates);

                UpdateAppearance(ent, ExperimentalDestructiveScannerVisualState.Idle);
            });

        if (completedCount > 0)
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-experimental-destructive-scanner-completed-named",
                ("count", completedCount),
                ("experiments", string.Join(", ", completedExperiments.Select(GetExperimentName))));
        }
        else if (changedAny)
            ent.Comp.LastResult = Loc.GetString("research-machine-experimental-destructive-scanner-progressed");
        else
            ent.Comp.LastResult = Loc.GetString("research-machine-experimental-destructive-scanner-no-matching-experiment");

        _chat.TrySendInGameICMessage(ent.Owner, Loc.GetString("research-machine-experimental-destructive-scanner-chat-result", ("result", ent.Comp.LastResult)), InGameICChatType.Speak, false);
        _audio.PlayPvs(changedAny || completedCount > 0
            ? ent.Comp.SuccessSound
            : ent.Comp.FailureSound,
            ent,
            ent.Comp.AudioParams);

        _research.LogNetworkEvent(server,
            "experimental-destructive-scanner",
            Loc.GetString("research-netlog-experimental-destructive-scanner-result",
                ("completed", completedCount),
                ("progressed", Loc.GetString(changedAny
                    ? "research-netlog-experimental-destructive-scanner-progress-yes"
                    : "research-netlog-experimental-destructive-scanner-progress-no")),
                ("user", _research.GetResearchLogUserName(user))),
            user);
        UpdateUi(ent);
    }

    private string GetExperimentName(string experimentId)
    {
        return _prototype.TryIndex<ResearchExperimentPrototype>(experimentId, out var prototype)
            ? Loc.GetString(prototype.Name)
            : experimentId;
    }

    private void UpdateAppearance(Entity<ExperimentalDestructiveScannerComponent> ent, ExperimentalDestructiveScannerVisualState state)
    {
        _appearance.SetData(ent.Owner, ExperimentalDestructiveScannerVisuals.State, state);
    }

    private void UpdateUi(Entity<ExperimentalDestructiveScannerComponent> ent)
    {
        string? serverName = null;
        List<ResearchPointAmount> pointBalances = [];
        List<ResearchMachineExperimentUiData> experiments = [];

        if (_research.TryResolveMachineServer(ent.Owner, out var serverUid) &&
            TryComp<ResearchServerComponent>(serverUid, out var server))
        {
            serverName = server.ServerName;
            pointBalances = server.PointBalances.ToList();

            if (TryComp<TechnologyDatabaseComponent>(serverUid, out var db))
            {
                foreach (var experimentId in db.ActiveExperiments)
                {
                    if (!_prototype.TryIndex<ResearchExperimentPrototype>(experimentId, out var prototype))
                        continue;

                    if (prototype.Hidden)
                        continue;

                    var progress = db.ExperimentProgress.FirstOrDefault(p => p.ExperimentId == experimentId);
                    experiments.Add(ResearchExperimentUiData.Create(prototype, progress, _prototype));
                }
            }
        }

        var status = ent.Comp.IsProcessing
            ? Loc.GetString("research-machine-experimental-destructive-scanner-state-processing")
            : Loc.GetString("research-machine-common-none");

        var state = new ExperimentalDestructiveScannerBoundInterfaceState(
            serverName,
            pointBalances,
            ent.Comp.LastSubject,
            ent.Comp.LastResult,
            experiments,
            status);

        _ui.SetUiState(ent.Owner, ExperimentalDestructiveScannerUiKey.Key, state);
    }


}
