// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.GameTicking;

namespace Content.Client.GameTicking;

public sealed partial class ClientGameTicker
{
    [ViewVariables] public string LobbyMapName { get; private set; } = string.Empty;
    [ViewVariables] public string LobbyModeTitle { get; private set; } = string.Empty;
    [ViewVariables] public int LobbyPlayerCount { get; private set; }
    [ViewVariables] public int LobbyReadyCount { get; private set; }
    [ViewVariables] public int LobbyInGameCount { get; private set; }

    private void ApplyNebulaLobbyInfo(TickerLobbyInfoEvent message)
    {
        LobbyMapName = message.MapName;
        LobbyModeTitle = message.ModeTitle;
        LobbyPlayerCount = message.PlayerCount;
        LobbyReadyCount = message.ReadyCount;
        LobbyInGameCount = message.InGameCount;
    }
}
