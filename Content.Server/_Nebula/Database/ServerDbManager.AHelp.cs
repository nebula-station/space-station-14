// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading.Tasks;

namespace Content.Server.Database;

public partial interface IServerDbManager
{
    Task AddAHelpMessage(AHelpMessage message);

    Task<List<AHelpMessage>> GetAHelpMessages(Guid ticketOwner, int? roundId);
}

public sealed partial class ServerDbManager
{
    public Task AddAHelpMessage(AHelpMessage message)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.AddAHelpMessage(message));
    }

    public Task<List<AHelpMessage>> GetAHelpMessages(Guid ticketOwner, int? roundId)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetAHelpMessages(ticketOwner, roundId));
    }
}
