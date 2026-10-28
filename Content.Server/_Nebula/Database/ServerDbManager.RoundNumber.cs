// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading.Tasks;

namespace Content.Server.Database;

public partial interface IServerDbManager
{
    Task<int?> FindRoundId(int serverId, int number);
}

public sealed partial class ServerDbManager
{
    public Task<int?> FindRoundId(int serverId, int number)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.FindRoundId(serverId, number));
    }
}
