// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.GameTicking;

namespace Content.Server.Administration.Logs;

public sealed partial class AdminLogsEui
{
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private ServerDbEntryManager _serverDbEntry = default!;

    private async Task<int?> ResolveRound(int number)
    {
        var ticker = _e.System<ServerGameTicker>();
        if (number == ticker.RoundId && ticker.RoundDatabaseId != 0)
            return ticker.RoundDatabaseId;

        var server = await _serverDbEntry.ServerEntity;
        return await _db.FindRoundId(server.Id, number);
    }
}
