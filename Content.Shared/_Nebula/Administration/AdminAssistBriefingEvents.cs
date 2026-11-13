// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Database;
using Content.Shared.Eui;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.Administration;

[Serializable, NetSerializable]
public sealed class AdminAssistBriefingEvent(
    NetUserId channel,
    long summaryId,
    int roundNumber,
    DateTime createdAt,
    string text,
    string? priority,
    bool unreliable,
    Dictionary<string, string> citedLines,
    HashSet<string> checkedCitations,
    List<NetUserId> participants,
    bool voted) : EntityEventArgs
{
    public readonly NetUserId Channel = channel;

    public readonly long SummaryId = summaryId;

    public readonly int RoundNumber = roundNumber;

    public readonly DateTime CreatedAt = createdAt;

    public readonly string Text = text;

    public readonly string? Priority = priority;

    public readonly bool Unreliable = unreliable;

    public readonly Dictionary<string, string> CitedLines = citedLines;

    public readonly HashSet<string> CheckedCitations = checkedCitations;

    public readonly List<NetUserId> Participants = participants;

    public readonly bool Voted = voted;
}

[Serializable, NetSerializable]
public sealed class AdminAssistVoteEvent(long summaryId, short vote) : EntityEventArgs
{
    public readonly long SummaryId = summaryId;
    public readonly short Vote = vote;
}

[Serializable, NetSerializable]
public sealed class AdminAssistOpenCitationEvent(long summaryId, string citation) : EntityEventArgs
{
    public readonly long SummaryId = summaryId;
    public readonly string Citation = citation;
}

[Serializable, NetSerializable]
public sealed class AdminAssistClaimEvent(NetUserId channel, bool take) : EntityEventArgs
{
    public readonly NetUserId Channel = channel;
    public readonly bool Take = take;
}

[Serializable, NetSerializable]
public sealed class AdminAssistCloseTicketEvent(NetUserId channel) : EntityEventArgs
{
    public readonly NetUserId Channel = channel;
}

[Serializable, NetSerializable]
public sealed class AdminAssistRequestBriefingEvent(NetUserId channel) : EntityEventArgs
{
    public readonly NetUserId Channel = channel;
}

[Serializable, NetSerializable]
public sealed class AdminAssistBriefingRequestedEvent(NetUserId channel, string? adminName) : EntityEventArgs
{
    public readonly NetUserId Channel = channel;

    // null — брифинг начался сам
    public readonly string? AdminName = adminName;
}

[Serializable, NetSerializable]
public sealed class AdminAssistTicketClosedEvent(NetUserId channel, long summaryId, string adminName) : EntityEventArgs
{
    public readonly NetUserId Channel = channel;
    public readonly long SummaryId = summaryId;
    public readonly string AdminName = adminName;
}

[Serializable, NetSerializable]
public sealed class AdminAssistClaimsEvent(Dictionary<NetUserId, AdminAssistClaim> claims) : EntityEventArgs
{
    public readonly Dictionary<NetUserId, AdminAssistClaim> Claims = claims;
}

[Serializable, NetSerializable]
public sealed class AdminAssistClaim(NetUserId admin, string adminName)
{
    public readonly NetUserId Admin = admin;
    public readonly string AdminName = adminName;
}

[Serializable, NetSerializable]
public sealed class AdminAssistOpenTicketLogsEvent(long summaryId) : EntityEventArgs
{
    public readonly long SummaryId = summaryId;
}

[Serializable, NetSerializable]
public sealed class AdminAssistTimelineRequestEvent(long summaryId) : EntityEventArgs
{
    public readonly long SummaryId = summaryId;
}

[Serializable, NetSerializable]
public sealed class AdminAssistTimelineEvent(long summaryId, List<AdminAssistTimelineEntry> entries) : EntityEventArgs
{
    public readonly long SummaryId = summaryId;
    public readonly List<AdminAssistTimelineEntry> Entries = entries;
}

[Serializable, NetSerializable]
public enum AdminAssistTimelineKind : byte
{
    Log,
    TicketOpened,
    LastMessage,
}

[Serializable, NetSerializable]
public sealed class AdminAssistTimelineEntry(
    DateTime time,
    AdminAssistTimelineKind kind,
    LogImpact impact,
    string text,
    bool cited)
{
    public readonly DateTime Time = time;
    public readonly AdminAssistTimelineKind Kind = kind;
    public readonly LogImpact Impact = impact;
    public readonly string Text = text;

    public readonly bool Cited = cited;
}

[Serializable, NetSerializable]
public sealed class AdminAssistLogFocusMessage(
    int roundNumber,
    List<Guid> players,
    DateTime after,
    DateTime before,
    HashSet<int> citedLogIds) : EuiMessageBase
{
    public readonly int RoundNumber = roundNumber;

    public readonly List<Guid> Players = players;

    public readonly DateTime After = after;

    public readonly DateTime Before = before;

    public readonly HashSet<int> CitedLogIds = citedLogIds;
}
