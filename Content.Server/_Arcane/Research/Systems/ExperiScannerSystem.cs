// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/02b6d617ada28d2a78cfd227b9860838fb082909/Content.Arcane.Server/Research/Systems/ExperiScannerSystem.cs

using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Server.Research.Systems;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Arcane.Research.Systems;

public sealed partial class ExperiScannerSystem : EntitySystem
{
    [Dependency] private ResearchSystem _research = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private ChatSystem _chat = default!;

    public override void Initialize()
    {
    }

    [SubscribeLocalEvent]
    private void OnUiOpened(Entity<ExperiScannerComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnResearchRegistrationChanged(Entity<ExperiScannerComponent> ent, ref ResearchRegistrationChangedEvent args)
    {
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnOpenServerMenu(Entity<ExperiScannerComponent> ent, ref OpenResearchServerMenuMessage args)
    {
        _ui.TryToggleUi(ent.Owner, ResearchClientUiKey.Key, args.Actor);
    }

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<ExperiScannerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target)
            return;

        if (!_interaction.InRangeUnobstructed(args.User, target, range: ent.Comp.ScanRange))
            return;

        args.Handled = true;

        if (!_research.TryResolveMachineServer(ent.Owner, out var server))
        {
            Fail(ent, args.User, "research-experi-scanner-no-server");
            return;
        }

        if (!_research.TryProgressExperimentsWithEntity(server,
                target,
                args.User,
                out var changed,
                out var completed,
                out var result,
                source: ExperimentSourceFlags.HandheldScanner))
        {
            var loc = result switch
            {
                ExperimentProgressAttemptResult.NoSourceCompatibleExperiment => "research-experi-scanner-no-compatible-experiments",
                ExperimentProgressAttemptResult.AlreadyScanned => "research-experi-scanner-already-scanned",
                _ => "research-experi-scanner-no-match",
            };

            Fail(ent, args.User, loc);
            return;
        }

        var targetName = Name(target);
        var popup = Loc.GetString("research-experi-scanner-progress", ("target", targetName));
        ent.Comp.LastResult = popup;

        _audio.PlayPvs(ent.Comp.SuccessSound, ent, AudioParams.Default.WithVolume(-2f));
        _popup.PopupEntity(popup, ent, args.User, PopupType.SmallCaution);
        _chat.TrySendInGameICMessage(ent.Owner, ent.Comp.LastResult, global::Content.Shared.Chat.InGameICChatType.Speak, false);
        UpdateUi(ent);

        _research.LogNetworkEvent(server,
            "experi-scanner",
            Loc.GetString("research-netlog-experi-scanner-scan",
                ("user", _research.GetResearchLogUserName(args.User)),
                ("scanner", Name(ent.Owner)),
                ("target", targetName),
                ("completed", completed.Count),
                ("progressed", Loc.GetString(changed ? "research-netlog-experimental-destructive-scanner-progress-yes" : "research-netlog-experimental-destructive-scanner-progress-no"))),
            args.User);
    }

    private void Fail(Entity<ExperiScannerComponent> ent, EntityUid user, string message)
    {
        ent.Comp.LastResult = Loc.GetString(message);
        _audio.PlayPvs(ent.Comp.FailureSound, ent, AudioParams.Default.WithVolume(-4f));
        _popup.PopupEntity(ent.Comp.LastResult, ent, user);
        UpdateUi(ent);
    }

    private void UpdateUi(Entity<ExperiScannerComponent> ent)
    {
        string? serverName = null;
        List<ResearchMachineExperimentUiData> experiments = [];

        if (_research.TryResolveMachineServer(ent.Owner, out var serverUid) &&
            TryComp<ResearchServerComponent>(serverUid, out var server) &&
            TryComp<TechnologyDatabaseComponent>(serverUid, out var db))
        {
            serverName = server.ServerName;

            foreach (var experimentId in db.ActiveExperiments)
            {
                if (!_prototype.TryIndex<ResearchExperimentPrototype>(experimentId, out var prototype) ||
                    prototype.Hidden ||
                    !prototype.SupportedSources.HasFlag(ExperimentSourceFlags.HandheldScanner))
                    continue;

                var progress = db.ExperimentProgress.FirstOrDefault(p => p.ExperimentId == experimentId);
                experiments.Add(ResearchExperimentUiData.Create(prototype, progress, _prototype));
            }
        }

        _ui.SetUiState(ent.Owner, ExperiScannerUiKey.Key, new ExperiScannerBoundInterfaceState(serverName, experiments, ent.Comp.LastResult));
    }
}
