// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Database;
using Content.Shared._Nebula.Administration;
using Content.Shared.Administration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Nebula.Administration.Systems;

public sealed partial class AdminAssistBriefingSystem
{
    private static readonly string?[] Priorities = ["ВЫСОКИЙ", "ОБЫЧНЫЙ", "НИЗКИЙ"];

    private readonly Dictionary<NetUserId, Claim> _claims = new();

    private sealed record Claim(NetUserId Admin, string AdminName, int RoundId);

    private readonly Dictionary<NetUserId, long> _closed = new();

    private void OnClaim(AdminAssistClaimEvent msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (!_enabled
            || _admin.GetAdminData(session) is not { } data
            || !data.HasFlag(AdminFlags.Adminhelp)
            || !_latest.TryGetValue(msg.Channel, out var summaryId)
            || _closed.ContainsKey(msg.Channel)
            || !TakeClick(session))
            return;

        if (msg.Take)
        {
            _claims[msg.Channel] = new Claim(session.UserId, session.Name, _briefings[summaryId].RoundId);
        }
        else
        {
            if (!_claims.TryGetValue(msg.Channel, out var claim) || claim.Admin != session.UserId)
                return;

            _claims.Remove(msg.Channel);
        }

        SendClaims();
    }

    private void OnCloseTicket(AdminAssistCloseTicketEvent msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (!_enabled
            || _admin.GetAdminData(session) is not { } data
            || !data.HasFlag(AdminFlags.Adminhelp)
            || !_latest.TryGetValue(msg.Channel, out var summaryId)
            || _closed.ContainsKey(msg.Channel)
            || !TakeClick(session))
            return;

        _closed[msg.Channel] = summaryId;
        _requested.Remove(msg.Channel);
        SendRequest(AHelpTicketRequestKind.Close, msg.Channel, session, _briefings[summaryId].RoundId);
        var closed = new AdminAssistTicketClosedEvent(msg.Channel, summaryId, session.Name);
        foreach (var admin in AHelpAdmins())
        {
            RaiseNetworkEvent(closed, admin);
        }

        if (_claims.Remove(msg.Channel))
            SendClaims();
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus == SessionStatus.Disconnected)
            ReleaseClaims(args.Session.UserId);
    }

    private void ReleaseClaims(NetUserId admin)
    {
        var channels = _claims.Where(c => c.Value.Admin == admin).Select(c => c.Key).ToList();
        if (channels.Count == 0)
            return;

        foreach (var channel in channels)
        {
            _claims.Remove(channel);
        }

        SendClaims();
    }

    private void ReleaseClaimFromOtherRound(NetUserId channel, int roundId)
    {
        if (_claims.TryGetValue(channel, out var claim) && claim.RoundId != roundId && _claims.Remove(channel))
            SendClaims();
    }

    private void ForgetTicketsWithoutBriefing()
    {
        foreach (var channel in _closed.Keys.Where(channel => !_latest.ContainsKey(channel)).ToList())
        {
            _closed.Remove(channel);
        }

        var gone = _claims.Keys.Where(channel => !_latest.ContainsKey(channel)).ToList();
        if (gone.Count == 0)
            return;

        foreach (var channel in gone)
        {
            _claims.Remove(channel);
        }

        SendClaims();
    }

    private void SendClaims()
    {
        var msg = ClaimsEvent();
        foreach (var admin in AHelpAdmins())
        {
            RaiseNetworkEvent(msg, admin);
        }
    }

    private AdminAssistClaimsEvent ClaimsEvent()
    {
        return new AdminAssistClaimsEvent(_claims.ToDictionary(
            c => c.Key,
            c => new AdminAssistClaim(c.Value.Admin, c.Value.AdminName)));
    }
}
