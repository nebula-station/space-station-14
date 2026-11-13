// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._Nebula.Administration;
using Content.Shared.Administration;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Nebula.Administration.Systems;

public sealed partial class AdminAssistBriefingSystem
{
    [Dependency] private ServerDbEntryManager _serverDbEntry = default!;

    private static readonly TimeSpan RequestWait = TimeSpan.FromMinutes(2);

    private readonly Dictionary<NetUserId, (string? AdminName, TimeSpan Until)> _requested = new();

    private const string ManualTrigger = "manual";
    private static readonly TimeSpan StartsLookback = TimeSpan.FromSeconds(15);

    private DateTime? _lastStartAt;
    private bool _startsFailed;

    private void OnRequestBriefing(AdminAssistRequestBriefingEvent msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (!_enabled
            || _admin.GetAdminData(session) is not { } data
            || !data.HasFlag(AdminFlags.Adminhelp)
            || HasOpenBriefing(msg.Channel)
            || !TakeClick(session))
            return;

        var now = _timing.RealTime;
        if (_requested.TryGetValue(msg.Channel, out var pending) && now < pending.Until)
        {
            RaiseNetworkEvent(new AdminAssistBriefingRequestedEvent(msg.Channel, pending.AdminName), session);
            return;
        }

        _requested[msg.Channel] = (session.Name, now + RequestWait);
        var requested = new AdminAssistBriefingRequestedEvent(msg.Channel, session.Name);
        foreach (var admin in AHelpAdmins())
        {
            RaiseNetworkEvent(requested, admin);
        }

        var roundId = _gameTicker.RoundDatabaseId;
        SendRequest(AHelpTicketRequestKind.Briefing, msg.Channel, session, roundId == 0 ? null : roundId);
    }

    private bool HasOpenBriefing(NetUserId channel)
    {
        return _latest.TryGetValue(channel, out var summaryId)
               && !_closed.ContainsKey(channel)
               && _briefings[summaryId].RoundId == _gameTicker.RoundDatabaseId;
    }

    private async Task PollStarts(int roundId)
    {
        try
        {
            var after = _lastStartAt ?? DateTime.UtcNow - StartsLookback;
            var rows = await _db.GetAdminAssistBriefingStarts(roundId, after, PollLimit);
            _startsFailed = false;
            foreach (var row in rows)
            {
                var startedAt = DateTime.SpecifyKind(row.StartedAt, DateTimeKind.Utc);
                _lastStartAt = _lastStartAt is { } last && last > startedAt ? last : startedAt;
                if (row.Trigger != ManualTrigger)
                    AutoStarted(new NetUserId(row.OwnerUserId));
            }

            _lastStartAt ??= after;
        }
        catch (Exception e)
        {
            if (!_startsFailed)
                _sawmill.Warning($"Could not read Admin Assist briefing starts: {e.Message}");
            _startsFailed = true;
        }
    }

    private void AutoStarted(NetUserId channel)
    {
        var now = _timing.RealTime;
        if (_requested.TryGetValue(channel, out var pending) && now < pending.Until)
            return;

        _requested[channel] = (null, now + RequestWait);
        var started = new AdminAssistBriefingRequestedEvent(channel, null);
        foreach (var admin in AHelpAdmins())
        {
            RaiseNetworkEvent(started, admin);
        }
    }

    private void BriefingArrived(NetUserId channel)
    {
        _requested.Remove(channel);
    }

    private async void SendRequest(AHelpTicketRequestKind kind, NetUserId channel, ICommonSession admin, int? roundId)
    {
        var request = new AHelpTicketRequest
        {
            RoundId = roundId,
            TicketOwnerUserId = channel,
            AdminUserId = admin.UserId,
            Kind = kind,
            RequestedAt = DateTime.UtcNow,
        };

        try
        {
            request.ServerId = (await _serverDbEntry.ServerEntity).Id;
            await _db.AddAHelpTicketRequest(request);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Could not store the Admin Assist {kind} request of {admin.Name}: {e.Message}");
            if (kind == AHelpTicketRequestKind.Briefing)
                _requested.Remove(channel);
        }
    }
}
