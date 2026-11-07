// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Server/_Orion/DocumentPrinter/DocumentPrinterSystem.cs

using Content.Shared._Nebula.Lathe;
using Content.Shared._Orion.DocumentPrinter;
using Content.Shared.Access.Components;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Paper;
using Content.Shared.PDA;
using Content.Trauma.Common.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Server._Orion.DocumentPrinter;

public sealed partial class DocumentPrinterSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private PaperSystem _paper = default!;

    private int _yearOffset;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, TraumaCVars.YearOffset, value => _yearOffset = value, true);
    }

    [SubscribeLocalEvent]
    private void OnQueued(Entity<DocumentPrinterComponent> ent, ref LatheRecipeQueuedEvent args)
    {
        var actors = args.Batch.Actors ??= [];
        var queuedBefore = args.Batch.ItemsRequested - args.Quantity;
        while (actors.Count < queuedBefore)
            actors.Add(EntityUid.Invalid);

        for (var i = 0; i < args.Quantity; i++)
            actors.Add(args.Actor);
    }

    [SubscribeLocalEvent]
    private void OnItemStarted(Entity<DocumentPrinterComponent> ent, ref LatheBatchItemStartedEvent args)
    {
        var index = args.Batch.ItemsPrinted - 1;
        var actors = args.Batch.Actors;
        ent.Comp.CurrentActor = actors != null && index >= 0 && index < actors.Count && actors[index].IsValid()
            ? actors[index]
            : null;
    }

    [SubscribeLocalEvent]
    private void OnItemRequeued(Entity<DocumentPrinterComponent> ent, ref LatheItemRequeuedEvent args)
    {
        args.Batch.Actors = [ent.Comp.CurrentActor ?? EntityUid.Invalid];
    }

    [SubscribeLocalEvent]
    private void OnProduced(Entity<DocumentPrinterComponent> ent, ref LatheProducedEntityEvent args)
    {
        var actor = ent.Comp.CurrentActor;
        ent.Comp.CurrentActor = null;

        if (!TryComp<PaperComponent>(args.Result, out var paper))
            return;

        IdCardComponent? idCard = null;
        PdaComponent? pda = null;
        var timeText = string.Empty;

        if (ent.Comp.IsOnAutocomplete)
        {
            if (actor != null && Exists(actor.Value) && _inventory.TryGetSlotEntity(actor.Value, "id", out var slot))
            {
                TryComp(slot, out pda);
                TryComp(pda?.ContainedId ?? slot, out idCard);
            }

            var elapsed = _gameTiming.CurTime - _ticker.RoundStartTimeSpan;
            var date = DateTime.Today.AddYears(_yearOffset);
            timeText = $@"{elapsed:hh\:mm\:ss} / {date:dd.MM.yyyy}";
        }

        var text = paper.Content.Replace("$time$", timeText);

        if (pda?.StationName is { } stationName)
            text = text.Replace("Station XX-000", stationName);

        _paper.SetContent((args.Result, paper), text
            .Replace("$name$", idCard?.FullName ?? string.Empty)
            .Replace("$job$", idCard?.LocalizedJobTitle ?? string.Empty));
    }
}
