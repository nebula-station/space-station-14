// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Nebula.Administration;
using Content.Shared.Database;
using Robust.Shared.Enums;

namespace Content.Server._Nebula.Administration.Systems;

public sealed partial class AdminAssistBriefingSystem
{
    private const int TimelineLimit = 12;

    private async void OnTimeline(AdminAssistTimelineRequestEvent msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (!TryGetLogsBriefing(session, msg.SummaryId, out var briefing) || !TakeClick(session))
            return;

        var after = briefing.OpenedAt - TicketBefore;
        var before = briefing.LastMessageAt + TicketAfter;
        List<Content.Server.Database.AdminLogTimelineLine> lines;
        try
        {
            lines = await _db.GetAdminAssistTimeline(
                briefing.RoundId,
                Participants(briefing).Select(p => p.UserId).ToList(),
                after,
                before,
                TimelineLimit);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Could not read the timeline of briefing {briefing.SummaryId}: {e}");
            return;
        }

        if (session.Status != SessionStatus.InGame && session.Status != SessionStatus.Connected)
            return;

        var cited = briefing.Citations.Values.SelectMany(c => c.LogIds).ToHashSet();
        var entries = lines
            .Select(line => new AdminAssistTimelineEntry(
                DateTime.SpecifyKind(line.Date, DateTimeKind.Utc),
                AdminAssistTimelineKind.Log,
                line.Impact,
                line.Message.Length > MaxLogLineLength ? line.Message[..MaxLogLineLength] + "…" : line.Message,
                cited.Contains(line.Id)))
            .Append(new AdminAssistTimelineEntry(briefing.OpenedAt, AdminAssistTimelineKind.TicketOpened, LogImpact.Low, "", false))
            .ToList();
        if (briefing.LastMessageAt > briefing.OpenedAt)
            entries.Add(new AdminAssistTimelineEntry(briefing.LastMessageAt, AdminAssistTimelineKind.LastMessage, LogImpact.Low, "", false));
        entries.Sort((a, b) => a.Time.CompareTo(b.Time));

        RaiseNetworkEvent(new AdminAssistTimelineEvent(briefing.SummaryId, entries), session);
    }
}
