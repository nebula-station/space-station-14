// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Client.UserInterface.Systems.Chat.Widgets;

public partial class ChatBox
{
    /// <summary>
    /// Forgets the last line after the contents are cleared, otherwise the first repopulated message
    /// coalesces with a line that is no longer there and removes an entry out of range.
    /// </summary>
    private void ResetCoalescence()
    {
        _lastLine = null;
        _lastLineRepeatCount = 0;
    }
}
