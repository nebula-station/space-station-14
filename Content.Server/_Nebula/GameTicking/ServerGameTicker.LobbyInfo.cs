// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.GameTicking;
using Content.Shared.Station.Components;
using Robust.Shared.Player;

namespace Content.Server.GameTicking;

public sealed partial class ServerGameTicker
{
    /// <summary>
    /// Set when a player joins the game, so a round start with many players sends the lobby info once.
    /// </summary>
    private bool _lobbyInfoQueued;

    /// <summary>
    /// Sends the queued lobby info only to players outside the round, players in it don't show the lobby bar.
    /// </summary>
    private void FlushLobbyInfo()
    {
        if (!_lobbyInfoQueued)
            return;

        _lobbyInfoQueued = false;

        var filter = Filter.Empty().AddWhere(session =>
            !_playerGameStatuses.TryGetValue(session.UserId, out var status) || status != PlayerGameStatus.JoinedGame);

        RaiseNetworkEvent(GetInfoMsg(), filter);
    }

    private TickerLobbyInfoEvent CreateLobbyInfoMsg(string textBlob)
    {
        var (ready, inGame) = CountLobbyPlayers();

        return new TickerLobbyInfoEvent(textBlob)
        {
            MapName = GetLobbyMapName(),
            ModeTitle = GetLobbyModeTitle(),
            PlayerCount = _playerManager.PlayerCount,
            ReadyCount = ready,
            InGameCount = inGame,
        };
    }

    /// <summary>
    /// Counts connected players only, game statuses keep disconnected players too.
    /// </summary>
    private (int Ready, int InGame) CountLobbyPlayers()
    {
        var ready = 0;
        var inGame = 0;

        foreach (var session in _playerManager.Sessions)
        {
            if (!_playerGameStatuses.TryGetValue(session.UserId, out var status))
                continue;

            if (status == PlayerGameStatus.ReadyToPlay)
                ready++;
            else if (status == PlayerGameStatus.JoinedGame)
                inGame++;
        }

        return (ready, inGame);
    }

    /// <summary>
    /// Plain station names for the lobby bar, or the selected map before the round has stations.
    /// </summary>
    private string GetLobbyMapName()
    {
        var names = new List<string>();
        var query = EntityQueryEnumerator<StationJobsComponent, StationSpawningComponent, MetaDataComponent>();

        while (query.MoveNext(out _, out _, out var meta))
        {
            names.Add(meta.EntityName);
        }

        if (names.Count > 0)
            return string.Join(", ", names);

        return _gameMapManager.GetSelectedMap()?.MapName ?? string.Empty;
    }

    private string GetLobbyModeTitle()
    {
        if (Decoy != null)
            return Loc.GetString(Decoy.ModeTitle);

        var preset = CurrentPreset ?? Preset;
        return preset == null ? string.Empty : Loc.GetString(preset.ModeTitle);
    }
}
