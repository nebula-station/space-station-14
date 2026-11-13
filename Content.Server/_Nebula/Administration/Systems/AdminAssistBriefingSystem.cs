// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Shared._Nebula.Administration;
using Content.Shared._Nebula.CCVar;
using Content.Shared.Administration;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Nebula.Administration.Systems;

public sealed partial class AdminAssistBriefingSystem : EntitySystem
{
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private EuiManager _eui = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ServerGameTicker _gameTicker = default!;
    [Dependency] private ISharedPlayerManager _player = default!;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan FailureBackoff = TimeSpan.FromSeconds(60);
    private const int PollLimit = 50;
    private const int MaxTextLength = 4000;
    private const int MaxLogIdsPerCitation = 3;
    private const int MaxLogLineLength = 500;
    private static readonly TimeSpan CitationWindow = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ClickCooldown = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan TicketBefore = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan TicketAfter = TimeSpan.FromSeconds(30);

    private ISawmill _sawmill = default!;
    private bool _enabled;
    private bool _polling;
    private TimeSpan _nextPoll;
    private long _lastSummaryId;
    private int _failures;

    private readonly Dictionary<long, Briefing> _briefings = new();

    private readonly Dictionary<NetUserId, long> _latest = new();

    private readonly HashSet<(long, NetUserId)> _votesInFlight = new();
    private readonly Dictionary<NetUserId, TimeSpan> _nextClick = new();

    private sealed record Briefing(
        long SummaryId,
        int RoundId,
        int RoundNumber,
        NetUserId Channel,
        DateTime CreatedAt,
        DateTime OpenedAt,
        DateTime LastMessageAt,
        string Text,
        string? Priority,
        bool Unreliable,
        Guid[] Participants,
        Dictionary<string, CitedLog> Citations);

    private sealed record CitedLog(string Text, HashSet<int> LogIds, DateTime First, DateTime Last);

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("admin_assist");
        Subs.CVar(_config, NCCVars.AdminAssistInGameBriefings, value => _enabled = value, true);
        SubscribeNetworkEvent<AdminAssistVoteEvent>(OnVote);
        SubscribeNetworkEvent<AdminAssistOpenCitationEvent>(OnOpenCitation);
        SubscribeNetworkEvent<AdminAssistOpenTicketLogsEvent>(OnOpenTicketLogs);
        SubscribeNetworkEvent<AdminAssistClaimEvent>(OnClaim);
        SubscribeNetworkEvent<AdminAssistCloseTicketEvent>(OnCloseTicket);
        SubscribeNetworkEvent<AdminAssistTimelineRequestEvent>(OnTimeline);
        SubscribeNetworkEvent<AdminAssistRequestBriefingEvent>(OnRequestBriefing);
        _admin.OnPermsChanged += OnPermsChanged;
        _player.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _admin.OnPermsChanged -= OnPermsChanged;
        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_enabled || _polling || _timing.RealTime < _nextPoll)
            return;

        var roundId = _gameTicker.RoundDatabaseId;
        if (roundId == 0)
            return;

        _nextPoll = _timing.RealTime + PollInterval;
        Poll(roundId, _gameTicker.RoundId);
    }

    private async void Poll(int roundId, int roundNumber)
    {
        _polling = true;
        try
        {
            await PollStarts(roundId);
            var rows = await _db.GetAdminAssistBriefings(roundId, _lastSummaryId, PollLimit);
            if (_failures > 0)
                _sawmill.Info("Reading briefings works again.");
            _failures = 0;

            DropOldRounds(roundId);
            foreach (var row in rows)
            {
                _lastSummaryId = Math.Max(_lastSummaryId, row.SummaryId);
                var briefing = new Briefing(
                    row.SummaryId,
                    roundId,
                    roundNumber,
                    new NetUserId(row.OwnerUserId),
                    DateTime.SpecifyKind(row.CreatedAt, DateTimeKind.Utc),
                    DateTime.SpecifyKind(row.OpenedAt, DateTimeKind.Utc),
                    DateTime.SpecifyKind(row.LastMessageAt, DateTimeKind.Utc),
                    row.Text.Length > MaxTextLength ? row.Text[..MaxTextLength] : row.Text,
                    Priorities.Contains(row.Priority) ? row.Priority : null,
                    row.Unreliable,
                    row.Participants,
                    await ReadCitations(roundId, row.Citations));

                _briefings[briefing.SummaryId] = briefing;
                _latest[briefing.Channel] = briefing.SummaryId;
                _closed.Remove(briefing.Channel);
                BriefingArrived(briefing.Channel);
                ReleaseClaimFromOtherRound(briefing.Channel, roundId);

                foreach (var admin in AHelpAdmins())
                {
                    RaiseNetworkEvent(ToEvent(briefing, admin, voted: false), admin);
                }
            }
        }
        catch (Exception e)
        {
            if (_failures++ % 10 == 0)
                _sawmill.Warning($"Could not read Admin Assist briefings (try {_failures}): {e.Message}");
            _nextPoll = _timing.RealTime + FailureBackoff;
        }
        finally
        {
            _polling = false;
        }
    }

    private async Task<Dictionary<string, CitedLog>> ReadCitations(int roundId, string citationsJson)
    {
        var citations = JsonSerializer.Deserialize<Dictionary<string, List<int>>>(citationsJson) ?? new();
        if (citations.Count == 0)
            return new Dictionary<string, CitedLog>();

        var ids = citations.Values.SelectMany(list => list.Take(MaxLogIdsPerCitation)).Distinct().ToList();
        var lines = await _db.GetAdminLogLines(roundId, ids);

        var cited = new Dictionary<string, CitedLog>();
        foreach (var (cite, logIds) in citations)
        {
            var found = logIds.Take(MaxLogIdsPerCitation).Where(lines.ContainsKey).ToList();
            if (found.Count == 0)
                continue;

            var text = string.Join("\n", found.Select(id => lines[id].Message.Length > MaxLogLineLength
                ? lines[id].Message[..MaxLogLineLength] + "…"
                : lines[id].Message));
            var dates = found.Select(id => DateTime.SpecifyKind(lines[id].Date, DateTimeKind.Utc)).ToList();
            cited[cite] = new CitedLog(text, found.ToHashSet(), dates.Min(), dates.Max());
        }

        return cited;
    }

    private void DropOldRounds(int roundId)
    {
        foreach (var (id, briefing) in _briefings.ToList())
        {
            if (briefing.RoundId >= roundId - 1)
                continue;

            _briefings.Remove(id);
            if (_latest.TryGetValue(briefing.Channel, out var latest) && latest == id)
                _latest.Remove(briefing.Channel);
        }

        ForgetTicketsWithoutBriefing();
    }

    private async void OnPermsChanged(Content.Server.Administration.AdminPermsChangedEventArgs args)
    {
        if (!args.IsAdmin || args.Flags is not { } held || !held.HasFlag(AdminFlags.Adminhelp))
            ReleaseClaims(args.Player.UserId);

        if (!_enabled || !args.IsAdmin || args.Flags is not { } flags || !flags.HasFlag(AdminFlags.Adminhelp))
            return;

        var briefings = _latest
            .Where(l => !_closed.ContainsKey(l.Key))
            .Select(l => _briefings[l.Value])
            .ToList();
        if (briefings.Count == 0)
            return;

        var session = args.Player;
        HashSet<long> voted;
        try
        {
            voted = (await _db.GetAHelpBriefingVotes(session.UserId, briefings.Select(b => b.SummaryId).ToList()))
                .ToHashSet();
        }
        catch (Exception e)
        {
            _sawmill.Error($"Could not read briefing votes of {session.Name}: {e}");
            return;
        }

        if (session.Status != SessionStatus.InGame && session.Status != SessionStatus.Connected)
            return;

        foreach (var briefing in briefings)
        {
            RaiseNetworkEvent(ToEvent(briefing, session, voted.Contains(briefing.SummaryId)), session);
        }

        RaiseNetworkEvent(ClaimsEvent(), session);
    }

    private async void OnVote(AdminAssistVoteEvent msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (_admin.GetAdminData(session) is not { } data || !data.HasFlag(AdminFlags.Adminhelp))
            return;

        if (msg.Vote is not (1 or -1) || !_briefings.ContainsKey(msg.SummaryId))
            return;

        var key = (msg.SummaryId, session.UserId);
        if (!_votesInFlight.Add(key))
            return;

        try
        {
            await _db.AddAHelpBriefingVote(new AHelpBriefingVote
            {
                SummaryId = msg.SummaryId,
                AdminUserId = session.UserId,
                Vote = msg.Vote,
                VotedAt = DateTime.UtcNow,
            });
        }
        catch (Exception e)
        {
            _sawmill.Error($"Could not store the briefing vote of {session.Name}: {e}");
        }
        finally
        {
            _votesInFlight.Remove(key);
        }
    }

    private void OnOpenCitation(AdminAssistOpenCitationEvent msg, EntitySessionEventArgs args)
    {
        if (!TryGetLogsBriefing(args.SenderSession, msg.SummaryId, out var briefing)
            || !briefing.Citations.TryGetValue(msg.Citation, out var cited)
            || !TakeClick(args.SenderSession))
            return;

        OpenLogs(args.SenderSession, briefing, cited.First - CitationWindow, cited.Last + CitationWindow, cited.LogIds);
    }

    private void OnOpenTicketLogs(AdminAssistOpenTicketLogsEvent msg, EntitySessionEventArgs args)
    {
        if (!TryGetLogsBriefing(args.SenderSession, msg.SummaryId, out var briefing)
            || !TakeClick(args.SenderSession))
            return;

        OpenLogs(args.SenderSession, briefing, briefing.OpenedAt - TicketBefore, briefing.LastMessageAt + TicketAfter, []);
    }

    private bool TryGetLogsBriefing(ICommonSession session, long summaryId, [NotNullWhen(true)] out Briefing? briefing)
    {
        briefing = null;
        if (_admin.GetAdminData(session) is not { } data
            || !data.HasFlag(AdminFlags.Adminhelp)
            || !data.HasFlag(AdminFlags.Logs))
            return false;

        return _briefings.TryGetValue(summaryId, out briefing);
    }

    private bool TakeClick(ICommonSession session)
    {
        var now = _timing.RealTime;
        if (_nextClick.TryGetValue(session.UserId, out var next) && now < next)
            return false;

        _nextClick[session.UserId] = now + ClickCooldown;
        return true;
    }

    private void OpenLogs(ICommonSession session, Briefing briefing, DateTime after, DateTime before, HashSet<int> cited)
    {
        var ui = new AdminLogsEui();
        _eui.OpenEui(ui, session);
        ui.FocusAdminAssistCitation(new AdminAssistLogFocusMessage(
            briefing.RoundNumber,
            Participants(briefing).Select(p => p.UserId).ToList(),
            after,
            before,
            cited));
    }

    private static List<NetUserId> Participants(Briefing briefing)
    {
        var players = new List<NetUserId> { briefing.Channel };
        foreach (var user in briefing.Participants)
        {
            if (user != briefing.Channel.UserId)
                players.Add(new NetUserId(user));
        }

        return players;
    }

    private AdminAssistBriefingEvent ToEvent(Briefing briefing, ICommonSession admin, bool voted)
    {
        var lines = _admin.GetAdminData(admin)?.HasFlag(AdminFlags.Logs) ?? false
            ? briefing.Citations.ToDictionary(c => c.Key, c => c.Value.Text)
            : new Dictionary<string, string>();

        return new AdminAssistBriefingEvent(
            briefing.Channel,
            briefing.SummaryId,
            briefing.RoundNumber,
            briefing.CreatedAt,
            briefing.Text,
            briefing.Priority,
            briefing.Unreliable,
            lines,
            briefing.Citations.Keys.ToHashSet(),
            Participants(briefing),
            voted);
    }

    private List<ICommonSession> AHelpAdmins()
    {
        return _admin.ActiveAdmins
            .Where(p => _admin.GetAdminData(p)?.HasFlag(AdminFlags.Adminhelp) ?? false)
            .ToList();
    }
}
