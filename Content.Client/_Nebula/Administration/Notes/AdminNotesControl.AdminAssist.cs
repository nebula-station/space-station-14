// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Client.Administration.UI.Notes;

public sealed partial class AdminNotesControl
{
    public void OpenAdminAssistNote(string text)
    {
        var noteEdit = new NoteEdit(null, PlayerName, CanCreate, CanEdit, CanWatchlist);
        noteEdit.SetAdminAssistText(text);
        noteEdit.SubmitPressed += OnNoteSubmitted;
        noteEdit.OpenCentered();
    }
}
