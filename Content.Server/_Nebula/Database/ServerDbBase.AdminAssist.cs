// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public sealed class AdminAssistBriefingRow
{
    [Column("summary_id")] public long SummaryId { get; set; }
    [Column("owner_user_id")] public Guid OwnerUserId { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }

    [Column("opened_at")] public DateTime OpenedAt { get; set; }
    [Column("last_message_at")] public DateTime LastMessageAt { get; set; }
    [Column("text")] public string Text { get; set; } = default!;
    [Column("priority")] public string? Priority { get; set; }
    [Column("unreliable")] public bool Unreliable { get; set; }

    [Column("citations")] public string Citations { get; set; } = default!;

    [Column("participants")] public Guid[] Participants { get; set; } = [];
}

public sealed class AdminAssistBriefingStartRow
{
    [Column("owner_user_id")] public Guid OwnerUserId { get; set; }
    [Column("briefing_started_at")] public DateTime StartedAt { get; set; }
    [Column("briefing_trigger")] public string? Trigger { get; set; }
}

public sealed record AdminLogLine(string Message, DateTime Date);

public sealed record AdminLogTimelineLine(int Id, LogImpact Impact, string Message, DateTime Date);

public abstract partial class ServerDbBase
{
    public virtual Task<List<AdminAssistBriefingRow>> GetAdminAssistBriefings(int roundId, long afterSummaryId, int limit)
    {
        return Task.FromResult(new List<AdminAssistBriefingRow>());
    }

    public virtual Task<List<AdminAssistBriefingStartRow>> GetAdminAssistBriefingStarts(int roundId, DateTime after, int limit)
    {
        return Task.FromResult(new List<AdminAssistBriefingStartRow>());
    }

    public async Task<Dictionary<int, AdminLogLine>> GetAdminLogLines(int roundId, List<int> logIds)
    {
        await using var db = await GetDb();

        return await db.DbContext.AdminLog
            .AsNoTracking()
            .Where(log => log.RoundId == roundId && logIds.Contains(log.Id))
            .ToDictionaryAsync(log => log.Id, log => new AdminLogLine(log.Message, log.Date));
    }

    private static readonly List<LogType> AdminAssistTimelineSkippedTypes =
    [
        LogType.Chat,
        LogType.Erp,
        LogType.ChatRateLimited,
        LogType.RateLimited,
        LogType.AdminCommands,
        LogType.AdminMessage,
        LogType.AdminFollow,
        LogType.GhostWarp,
        LogType.Connection,
        LogType.Vote,
        LogType.RoundStartJoin,
        LogType.LateJoin,
        LogType.Respawn,
        LogType.GhostRoleTaken,
        LogType.AntagSelection,
        LogType.Identity,
        LogType.Mind,
        LogType.Action,
    ];

    public async Task<List<AdminLogTimelineLine>> GetAdminAssistTimeline(
        int roundId,
        List<Guid> players,
        DateTime after,
        DateTime before,
        int limit)
    {
        await using var db = await GetDb();

        return await db.DbContext.AdminLog
            .AsNoTracking()
            .Where(log => log.RoundId == roundId
                          && log.Date >= after
                          && log.Date <= before
                          && log.Impact >= LogImpact.Medium
                          && !AdminAssistTimelineSkippedTypes.Contains(log.Type)
                          && log.Players.Any(p => players.Contains(p.PlayerUserId)))
            .OrderByDescending(log => log.Players.Count(p => players.Contains(p.PlayerUserId)) >= 2)
            .ThenByDescending(log => log.Impact)
            .ThenBy(log => log.Date)
            .Take(limit)
            .Select(log => new AdminLogTimelineLine(log.Id, log.Impact, log.Message, log.Date))
            .ToListAsync();
    }

    public async Task<bool> AddAHelpBriefingVote(AHelpBriefingVote vote)
    {
        await using var db = await GetDb();

        if (await db.DbContext.AHelpBriefingVote.AnyAsync(v =>
                v.SummaryId == vote.SummaryId && v.AdminUserId == vote.AdminUserId))
            return false;

        db.DbContext.AHelpBriefingVote.Add(vote);
        try
        {
            await db.DbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return false;
        }

        return true;
    }

    public async Task<List<long>> GetAHelpBriefingVotes(Guid admin, List<long> summaryIds)
    {
        await using var db = await GetDb();

        return await db.DbContext.AHelpBriefingVote
            .AsNoTracking()
            .Where(v => v.AdminUserId == admin && summaryIds.Contains(v.SummaryId))
            .Select(v => v.SummaryId)
            .ToListAsync();
    }

    public async Task AddAHelpTicketRequest(AHelpTicketRequest request)
    {
        await using var db = await GetDb();

        db.DbContext.AHelpTicketRequest.Add(request);
        await db.DbContext.SaveChangesAsync();
    }
}

public sealed partial class ServerDbPostgres
{
    public override async Task<List<AdminAssistBriefingRow>> GetAdminAssistBriefings(
        int roundId,
        long afterSummaryId,
        int limit)
    {
        await using var db = await GetDbImpl();

        return await db.PgDbContext.Database.SqlQuery<AdminAssistBriefingRow>($"""
SELECT s.summary_id, t.owner_user_id, s.created_at, t.opened_at, t.last_message_at, s.text, s.priority, s.unreliable, s.citations::text AS citations, s.participants
FROM admin_assist.ahelp_summary AS s
JOIN admin_assist.ahelp_ticket AS t ON t.ticket_id = s.ticket_id
WHERE t.round_id = {roundId} AND s.summary_id > {afterSummaryId}
ORDER BY s.summary_id
LIMIT {limit}
""").ToListAsync();
    }

    public override async Task<List<AdminAssistBriefingStartRow>> GetAdminAssistBriefingStarts(
        int roundId,
        DateTime after,
        int limit)
    {
        await using var db = await GetDbImpl();

        return await db.PgDbContext.Database.SqlQuery<AdminAssistBriefingStartRow>($"""
SELECT owner_user_id, briefing_started_at, briefing_trigger
FROM admin_assist.ahelp_ticket
WHERE round_id = {roundId} AND briefing_started_at > {after}
ORDER BY briefing_started_at
LIMIT {limit}
""").ToListAsync();
    }
}
