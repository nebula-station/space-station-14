// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Client.Administration.UI.Notes;

public sealed partial class NoteEdit
{
    public void SetAdminAssistText(string text)
    {
        NoteTextEdit.InsertAtCursor(text);
    }
}
