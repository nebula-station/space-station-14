// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Utility;

namespace Content.Client.Administration.UI.Bwoink;

public sealed partial class BwoinkPanel
{
    public void SetAdminAssistDraft(string draft)
    {
        SenderLineEdit.Text = draft;
        SenderLineEdit.CursorPosition = draft.Length;
        SenderLineEdit.GrabKeyboardFocus();
    }

    public void AddAdminAssistNote(string text)
    {
        var formatted = new FormattedMessage(1);
        formatted.PushColor(Color.Gray);
        formatted.AddText($"{DateTime.Now.ToShortTimeString()} {text}");
        formatted.Pop();
        TextOutput.AddMessage(formatted);
    }
}
