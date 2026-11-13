// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.Administration.Managers;
using Content.Client.Administration.Systems;
using Content.Client.Administration.UI.Bwoink;
using Content.Client.Administration.UI.CustomControls;
using Content.Client.UserInterface.Systems.Bwoink;
using Content.Shared._Nebula.Administration;
using Content.Shared._Nebula.CCVar;
using Content.Shared.Administration;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client._Nebula.Administration;

public sealed partial class AdminAssistBriefingSystem : EntitySystem
{
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private IClientAdminManager _admin = default!;
    [Dependency] private IClientConsoleHost _console = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private AdminSystem _adminSystem = default!;

    private static readonly TimeSpan NoteDraftTimeout = TimeSpan.FromSeconds(15);

    private static readonly TimeSpan RequestWait = TimeSpan.FromMinutes(2);

    private readonly Dictionary<NetUserId, AdminAssistBriefingEvent> _briefings = new();
    private readonly HashSet<long> _voted = new();
    private Dictionary<NetUserId, AdminAssistClaim> _claims = new();
    private readonly Dictionary<NetUserId, TimeSpan> _requested = new();

    private IAHelpUIHandler? _shownIn;

    private BwoinkControl? _listShownIn;

    private (string Username, string Draft, TimeSpan Until)? _noteDraft;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AdminAssistBriefingEvent>(OnBriefing);
        SubscribeNetworkEvent<AdminAssistClaimsEvent>(OnClaims);
        SubscribeNetworkEvent<AdminAssistTimelineEvent>(OnTimeline);
        SubscribeNetworkEvent<AdminAssistTicketClosedEvent>(OnTicketClosed);
        SubscribeNetworkEvent<AdminAssistBriefingRequestedEvent>(OnBriefingRequested);
        _adminSystem.PlayerListChanged += OnPlayerListChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _adminSystem.PlayerListChanged -= OnPlayerListChanged;
    }

    private void OnPlayerListChanged(List<PlayerInfo> players)
    {
        foreach (var briefing in _briefings.Values)
        {
            if (FindControl(briefing.Channel) is { } control)
                control.SetPlayers(players);
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var helper = _ui.GetUIController<AHelpUIController>().UIHelper;
        if (helper != _shownIn)
        {
            _shownIn = helper;
            foreach (var briefing in _briefings.Values)
            {
                Show(briefing);
            }
        }

        if (helper is AdminAHelpUIHandler { Control: { Disposed: false } bwoink } && bwoink != _listShownIn)
        {
            _listShownIn = bwoink;
            HookChannelList(bwoink.ChannelSelector);
            bwoink.ChannelSelector.OnSelectionChanged += selected =>
            {
                if (selected != null)
                    EnsureRequestRow(selected.SessionId);
            };
        }

        if (_requested.Count == 0)
            return;

        foreach (var channel in _requested.Where(r => _timing.RealTime >= r.Value).Select(r => r.Key).ToList())
        {
            _requested.Remove(channel);
            ShowRequested(channel);
        }
    }

    private void HookChannelList(PlayerListControl list)
    {
        var order = list.Comparison;
        list.Comparison = (a, b) =>
        {
            if (a.IsPinned != b.IsPinned)
                return a.IsPinned ? -1 : 1;

            var rank = ListRank(a.SessionId).CompareTo(ListRank(b.SessionId));
            if (rank != 0)
                return rank;

            return order?.Invoke(a, b) ?? 0;
        };
        list.PlayerListContainer.GenerateItem += (data, button) =>
        {
            if (data is PlayerListData { Info: var info }
                && OpenTicket(info.SessionId) is { } briefing
                && button.ChildCount > 0
                && button.GetChild(0) is PlayerListEntry entry)
            {
                var claim = _claims.GetValueOrDefault(info.SessionId);
                var stripe = AdminAssistPriority.Stripe(briefing.Priority, claim?.AdminName);
                entry.AddChild(stripe);
                stripe.SetPositionInParent(0);
            }
        };
        list.PopulateList();
    }

    private void RefreshChannelList()
    {
        if (_listShownIn is { Disposed: false } bwoink)
            bwoink.ChannelSelector.PopulateList();
    }

    private AdminAssistBriefingEvent? OpenTicket(NetUserId channel)
    {
        if (!_briefings.TryGetValue(channel, out var briefing))
            return null;

        return briefing.RoundNumber == _briefings.Values.Max(b => b.RoundNumber) ? briefing : null;
    }

    private int ListRank(NetUserId channel)
    {
        return OpenTicket(channel) is { } briefing
            ? AdminAssistPriority.Rank(briefing.Priority)
            : AdminAssistPriority.NoTicket;
    }

    private void OnBriefing(AdminAssistBriefingEvent msg)
    {
        if (_briefings.TryGetValue(msg.Channel, out var current) && current.SummaryId > msg.SummaryId)
            return;

        _briefings[msg.Channel] = msg;
        _requested.Remove(msg.Channel);
        if (msg.Voted)
            _voted.Add(msg.SummaryId);
        Show(msg);
        RefreshChannelList();
        foreach (var briefing in _briefings.Values)
        {
            if (FindControl(briefing.Channel) is { } control)
                control.CanRequest = OpenTicket(briefing.Channel) == null;
        }
    }

    private void OnTicketClosed(AdminAssistTicketClosedEvent msg)
    {
        if (!_briefings.TryGetValue(msg.Channel, out var briefing) || briefing.SummaryId > msg.SummaryId)
            return;

        _briefings.Remove(msg.Channel);
        _claims.Remove(msg.Channel);
        _requested.Remove(msg.Channel);
        if (_ui.GetUIController<AHelpUIController>().UIHelper is AdminAHelpUIHandler helper)
        {
            var panel = helper.EnsurePanel(msg.Channel);
            panel.AdminAssistArea.RemoveAllChildren();
            panel.AdminAssistArea.AddChild(RequestRow(msg.Channel));
            panel.AddAdminAssistNote(Loc.GetString("admin-assist-ticket-closed", ("admin", msg.AdminName)));
        }

        RefreshChannelList();
    }

    private void EnsureRequestRow(NetUserId channel)
    {
        if (!_cfg.GetCVar(NCCVars.AdminAssistInGameBriefings)
            || _ui.GetUIController<AHelpUIController>().UIHelper is not AdminAHelpUIHandler helper)
            return;

        var area = helper.EnsurePanel(channel).AdminAssistArea;
        if (area.ChildCount == 0)
            area.AddChild(RequestRow(channel));
    }

    private AdminAssistRequestRow RequestRow(NetUserId channel)
    {
        var row = new AdminAssistRequestRow(channel);
        row.OnRequestBriefing += RequestBriefing;
        row.SetRequested(Requested(channel));
        return row;
    }

    private void RequestBriefing(NetUserId channel)
    {
        RaiseNetworkEvent(new AdminAssistRequestBriefingEvent(channel));
    }

    private void OnBriefingRequested(AdminAssistBriefingRequestedEvent msg)
    {
        var fresh = _requested.TryAdd(msg.Channel, _timing.RealTime + RequestWait);
        ShowRequested(msg.Channel);
        if (!fresh || _ui.GetUIController<AHelpUIController>().UIHelper is not AdminAHelpUIHandler helper)
            return;

        helper.EnsurePanel(msg.Channel).AddAdminAssistNote(msg.AdminName == null
            ? Loc.GetString("admin-assist-briefing-started")
            : Loc.GetString("admin-assist-briefing-requested", ("admin", msg.AdminName)));
    }

    private bool Requested(NetUserId channel)
    {
        return _requested.ContainsKey(channel);
    }

    private void ShowRequested(NetUserId channel)
    {
        if (_ui.GetUIController<AHelpUIController>().UIHelper is not AdminAHelpUIHandler helper)
            return;

        var area = helper.EnsurePanel(channel).AdminAssistArea;
        switch (area.ChildCount > 0 ? area.GetChild(0) : null)
        {
            case AdminAssistBriefingControl control:
                control.SetRequested(Requested(channel));
                break;
            case AdminAssistRequestRow row:
                row.SetRequested(Requested(channel));
                break;
        }
    }

    private void OnTimeline(AdminAssistTimelineEvent msg)
    {
        foreach (var briefing in _briefings.Values)
        {
            if (briefing.SummaryId == msg.SummaryId
                && FindControl(briefing.Channel) is { } control
                && control.SummaryId == msg.SummaryId)
                control.SetTimeline(msg.Entries);
        }
    }

    private void OnClaims(AdminAssistClaimsEvent msg)
    {
        _claims = msg.Claims;
        foreach (var briefing in _briefings.Values)
        {
            FindControl(briefing.Channel)?.SetClaim(_claims.GetValueOrDefault(briefing.Channel), Me());
        }

        RefreshChannelList();
    }

    private NetUserId Me()
    {
        return _player.LocalSession?.UserId ?? default;
    }

    private void Claim(NetUserId channel, bool take)
    {
        RaiseNetworkEvent(new AdminAssistClaimEvent(channel, take));
    }

    private void Show(AdminAssistBriefingEvent briefing)
    {
        if (_ui.GetUIController<AHelpUIController>().UIHelper is not AdminAHelpUIHandler helper)
            return;

        var area = helper.EnsurePanel(briefing.Channel).AdminAssistArea;
        if (area.ChildCount == 0 || area.GetChild(0) is not AdminAssistBriefingControl control)
        {
            area.RemoveAllChildren();
            control = new AdminAssistBriefingControl();
            control.OnVote += Vote;
            control.OnReply += Reply;
            control.OnTicketLogs += id => RaiseNetworkEvent(new AdminAssistOpenTicketLogsEvent(id));
            control.OnTimeline += id => RaiseNetworkEvent(new AdminAssistTimelineRequestEvent(id));
            control.OnFollow += player => _console.ExecuteCommand($"follow \"{player.NetEntity}\"");
            control.OnNote += OpenNote;
            control.OnClaim += Claim;
            control.OnClose += channel => RaiseNetworkEvent(new AdminAssistCloseTicketEvent(channel));
            control.OnRequestBriefing += RequestBriefing;
            area.AddChild(control);
        }

        control.CanLogs = _admin.HasFlag(AdminFlags.Logs);
        control.CanNotes = _admin.HasFlag(AdminFlags.ViewNotes);
        control.CanFollow = _admin.CanCommand("follow");
        control.CanPlayerPanel = _admin.CanCommand("playerpanel");
        control.SetBriefing(briefing, _voted.Contains(briefing.SummaryId));
        control.SetPlayers(_adminSystem.PlayerList);
        control.SetClaim(_claims.GetValueOrDefault(briefing.Channel), Me());
        control.SetRequested(Requested(briefing.Channel));
        control.CanRequest = OpenTicket(briefing.Channel) == null;
    }

    private AdminAssistBriefingControl? FindControl(NetUserId channel)
    {
        if (_ui.GetUIController<AHelpUIController>().UIHelper is not AdminAHelpUIHandler helper)
            return null;

        var area = helper.EnsurePanel(channel).AdminAssistArea;
        return area.ChildCount > 0 ? area.GetChild(0) as AdminAssistBriefingControl : null;
    }

    private void Reply(NetUserId channel, string draft)
    {
        if (_ui.GetUIController<AHelpUIController>().UIHelper is AdminAHelpUIHandler helper)
            helper.EnsurePanel(channel).SetAdminAssistDraft(draft);
    }

    private void OpenNote(PlayerInfo player, string draft)
    {
        _noteDraft = (player.Username, draft, _timing.RealTime + NoteDraftTimeout);
        _console.ExecuteCommand($"adminnotes \"{player.SessionId}\"");
    }

    public string? TakeNoteDraft(string username)
    {
        if (_noteDraft is not { } pending || pending.Username != username || _timing.RealTime > pending.Until)
            return null;

        _noteDraft = null;
        return pending.Draft;
    }

    public void OpenCitation(long summaryId, string citation)
    {
        RaiseNetworkEvent(new AdminAssistOpenCitationEvent(summaryId, citation));
    }

    private void Vote(long summaryId, short vote)
    {
        _voted.Add(summaryId);
        RaiseNetworkEvent(new AdminAssistVoteEvent(summaryId, vote));
    }
}
