// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Client.Lobby;

public sealed partial class LobbyUIController
{
    /// <summary>
    /// Tab of <see cref="UI.HumanoidProfileEditor"/> that loadouts are opened from.
    /// </summary>
    public const int JobsTab = 1;

    public void SelectProfileEditorTab(int index)
    {
        var (_, editor) = EnsureGui();
        editor.TabContainer.CurrentTab = index;
    }
}
