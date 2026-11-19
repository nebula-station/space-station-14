// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Orion.DocumentPrinter;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Trauma.Common.Materials;

namespace Content.Shared._Nebula.DocumentPrinter;

public sealed partial class DocumentPrinterPaperSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DocumentPrinterComponent, MaterialStorageInsertAttemptEvent>(OnInsertAttempt);
    }

    private void OnInsertAttempt(Entity<DocumentPrinterComponent> ent, ref MaterialStorageInsertAttemptEvent args)
    {
        if (!TryComp<PaperComponent>(args.Item, out var paper))
            return;

        if (string.IsNullOrWhiteSpace(paper.Content) && paper.StampedBy.Count == 0 && paper.StampState == null)
            return;

        args.Handled = true;
        args.Cancelled = true;
        _popup.PopupClient(Loc.GetString("document-printer-paper-not-blank"), ent, args.User);
    }
}
