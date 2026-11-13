// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading.Tasks;

namespace Content.Server.Database;

public partial interface IServerDbManager
{
    Task<List<AdminAssistBriefingRow>> GetAdminAssistBriefings(int roundId, long afterSummaryId, int limit);

    Task<List<AdminAssistBriefingStartRow>> GetAdminAssistBriefingStarts(int roundId, DateTime after, int limit);

    Task<Dictionary<int, AdminLogLine>> GetAdminLogLines(int roundId, List<int> logIds);

    Task<List<AdminLogTimelineLine>> GetAdminAssistTimeline(
        int roundId,
        List<Guid> players,
        DateTime after,
        DateTime before,
        int limit);

    Task<bool> AddAHelpBriefingVote(AHelpBriefingVote vote);

    Task<List<long>> GetAHelpBriefingVotes(Guid admin, List<long> summaryIds);

    Task AddAHelpTicketRequest(AHelpTicketRequest request);
}

public sealed partial class ServerDbManager
{
    public Task<List<AdminAssistBriefingRow>> GetAdminAssistBriefings(int roundId, long afterSummaryId, int limit)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetAdminAssistBriefings(roundId, afterSummaryId, limit));
    }

    public Task<List<AdminAssistBriefingStartRow>> GetAdminAssistBriefingStarts(int roundId, DateTime after, int limit)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetAdminAssistBriefingStarts(roundId, after, limit));
    }

    public Task<Dictionary<int, AdminLogLine>> GetAdminLogLines(int roundId, List<int> logIds)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetAdminLogLines(roundId, logIds));
    }

    public Task<List<AdminLogTimelineLine>> GetAdminAssistTimeline(
        int roundId,
        List<Guid> players,
        DateTime after,
        DateTime before,
        int limit)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetAdminAssistTimeline(roundId, players, after, before, limit));
    }

    public Task<bool> AddAHelpBriefingVote(AHelpBriefingVote vote)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.AddAHelpBriefingVote(vote));
    }

    public Task<List<long>> GetAHelpBriefingVotes(Guid admin, List<long> summaryIds)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetAHelpBriefingVotes(admin, summaryIds));
    }

    public Task AddAHelpTicketRequest(AHelpTicketRequest request)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.AddAHelpTicketRequest(request));
    }
}
