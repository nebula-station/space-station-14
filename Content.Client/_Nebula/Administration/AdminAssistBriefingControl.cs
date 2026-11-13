// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Content.Client.UserInterface.Controls;
using Content.Shared._Nebula.Administration;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Network;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.Administration;

public sealed class AdminAssistBriefingControl : BoxContainer
{
    private static readonly Type[] AllowedTags =
        [typeof(AdminAssistCiteTag), typeof(AdminAssistPlayerTag), typeof(BoldTag), typeof(ColorTag)];

    private const int MinNameLength = 3;
    private const float TimelineHeight = 150;
    private const float BodyMaxHeight = 200;
    private const float MinScrollHeight = 48;
    private const float BodyShare = 0.3f;
    private const float TimelineShare = 0.2f;
    private const float PlayerMaxWidth = 260;

    private static readonly Color Confirmed = Color.LimeGreen;
    private static readonly Color Contradicted = Color.Red;
    private static readonly Color Unconfirmed = Color.Yellow;
    private static readonly Color CitedColor = Color.Gold;

    private static readonly Regex FieldRegex = new(@"^([А-ЯЁA-Z][а-яёa-z]+(?: [а-яёa-z]+)?):", RegexOptions.Compiled);
    private static readonly Regex CiteRegex = new(@"\bL\d+\b", RegexOptions.Compiled);

    private readonly Label _title;
    private readonly Label _claimedBy;
    private readonly Button _claim;
    private readonly ConfirmButton _close;
    private readonly Button _request;
    private readonly Button _toggle;
    private readonly Button _up;
    private readonly Button _down;
    private readonly Label _voted;
    private readonly RichTextLabel _body;
    private readonly ScrollContainer _bodyScroll;
    private readonly BoxContainer _actions;
    private readonly Button _reply;
    private readonly Button _ticketLogs;
    private readonly Button _timeline;
    private readonly BoxContainer _timelineBox;
    private readonly ScrollContainer _timelineScroll;
    private readonly OptionButton _player;
    private readonly Button _follow;
    private readonly Button _note;

    private AdminAssistBriefingEvent? _briefing;
    private bool _claimMine;
    private Dictionary<string, NetUserId> _names = new();
    private readonly List<PlayerInfo> _players = new();

    public long SummaryId { get; private set; }

    public bool CanLogs { get; set; }
    public bool CanNotes { get; set; }
    public bool CanFollow { get; set; }
    public bool CanPlayerPanel { get; set; }

    public bool CanRequest
    {
        get => _request.Visible;
        set => _request.Visible = value;
    }

    public event Action<long, short>? OnVote;

    public event Action<NetUserId, string>? OnReply;

    public event Action<long>? OnTicketLogs;

    public event Action<long>? OnTimeline;

    public event Action<PlayerInfo>? OnFollow;

    public event Action<PlayerInfo, string>? OnNote;

    public event Action<NetUserId, bool>? OnClaim;

    public event Action<NetUserId>? OnClose;

    public event Action<NetUserId>? OnRequestBriefing;

    public AdminAssistBriefingControl()
    {
        Orientation = LayoutOrientation.Vertical;
        Margin = new Thickness(0, 0, 0, 4);

        _title = new Label { HorizontalExpand = true, StyleClasses = { "LabelSubText" } };
        _claimedBy = new Label { StyleClasses = { "LabelSubText" }, Margin = new Thickness(0, 0, 4, 0), Visible = false };
        _claim = new Button
        {
            ToolTip = Loc.GetString("admin-assist-claim-tooltip"),
            StyleClasses = { "OpenRight" },
        };
        _close = new ConfirmButton
        {
            Text = Loc.GetString("admin-assist-close"),
            ConfirmationText = Loc.GetString("admin-assist-close-confirm"),
            ToolTip = Loc.GetString("admin-assist-close-tooltip"),
            StyleClasses = { "OpenLeft" },
            Margin = new Thickness(0, 0, 4, 0),
        };
        _request = new Button
        {
            Text = Loc.GetString("admin-assist-request"),
            ToolTip = Loc.GetString("admin-assist-request-tooltip"),
            Margin = new Thickness(0, 0, 4, 0),
        };
        _toggle = new Button { Text = Loc.GetString("admin-assist-briefing-hide"), StyleClasses = { "OpenRight" } };
        _up = new Button
        {
            Text = "👍",
            ToolTip = Loc.GetString("admin-assist-briefing-vote-up"),
            StyleClasses = { "OpenBoth" },
        };
        _down = new Button
        {
            Text = "👎",
            ToolTip = Loc.GetString("admin-assist-briefing-vote-down"),
            StyleClasses = { "OpenLeft" },
        };
        _voted = new Label
        {
            Text = Loc.GetString("admin-assist-briefing-voted"),
            StyleClasses = { "LabelSubText" },
            Visible = false,
        };
        _body = new RichTextLabel { Margin = new Thickness(4, 2) };
        _bodyScroll = new ScrollContainer { MaxHeight = BodyMaxHeight, HScrollEnabled = false, ReturnMeasure = true };
        _bodyScroll.AddChild(_body);

        _reply = new Button
        {
            Text = Loc.GetString("admin-assist-briefing-reply"),
            ToolTip = Loc.GetString("admin-assist-briefing-reply-tooltip"),
            StyleClasses = { "OpenRight" },
        };
        _ticketLogs = new Button
        {
            Text = Loc.GetString("admin-assist-briefing-ticket-logs"),
            ToolTip = Loc.GetString("admin-assist-briefing-ticket-logs-tooltip"),
            StyleClasses = { "OpenBoth" },
        };
        _timeline = new Button
        {
            Text = Loc.GetString("admin-assist-briefing-timeline"),
            ToolTip = Loc.GetString("admin-assist-briefing-timeline-tooltip"),
            StyleClasses = { "OpenLeft" },
            ToggleMode = true,
        };
        _timelineBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Margin = new Thickness(8, 0, 4, 2),
        };
        _timelineScroll = new ScrollContainer { SetHeight = TimelineHeight, HScrollEnabled = false, Visible = false };
        _timelineScroll.AddChild(_timelineBox);
        _player = new OptionButton
        {
            HorizontalExpand = true,
            MaxWidth = PlayerMaxWidth,
            RectClipContent = true,
            StyleClasses = { "OpenRight" },
        };
        _follow = new Button { Text = Loc.GetString("admin-player-actions-follow"), StyleClasses = { "OpenBoth" } };
        _note = new Button
        {
            Text = Loc.GetString("admin-assist-briefing-note"),
            ToolTip = Loc.GetString("admin-assist-briefing-note-tooltip"),
            StyleClasses = { "OpenLeft" },
        };

        _actions = new BoxContainer { Orientation = LayoutOrientation.Horizontal, Margin = new Thickness(4, 0, 4, 2) };
        _actions.AddChild(_reply);
        _actions.AddChild(_ticketLogs);
        _actions.AddChild(_timeline);
        _actions.AddChild(new Control { MinWidth = 8 });
        _actions.AddChild(_player);
        _actions.AddChild(_follow);
        _actions.AddChild(_note);

        var header = new BoxContainer { Orientation = LayoutOrientation.Horizontal };
        header.AddChild(_title);
        header.AddChild(_claimedBy);
        header.AddChild(_claim);
        header.AddChild(_close);
        header.AddChild(_request);
        header.AddChild(_voted);
        header.AddChild(_toggle);
        header.AddChild(_up);
        header.AddChild(_down);

        AddChild(header);
        AddChild(_bodyScroll);
        AddChild(_actions);
        AddChild(_timelineScroll);

        _toggle.OnPressed += _ =>
        {
            _bodyScroll.Visible = !_bodyScroll.Visible;
            _actions.Visible = _bodyScroll.Visible;
            _timelineScroll.Visible = _bodyScroll.Visible && _timeline.Pressed;
            _toggle.Text = Loc.GetString(_bodyScroll.Visible ? "admin-assist-briefing-hide" : "admin-assist-briefing-show");
        };
        _claim.OnPressed += _ =>
        {
            if (_briefing != null)
                OnClaim?.Invoke(_briefing.Channel, !_claimMine);
        };
        _close.OnPressed += _ =>
        {
            if (_briefing != null)
                OnClose?.Invoke(_briefing.Channel);
        };
        _request.OnPressed += _ =>
        {
            if (_briefing != null)
                OnRequestBriefing?.Invoke(_briefing.Channel);
        };
        _up.OnPressed += _ => Vote(1);
        _down.OnPressed += _ => Vote(-1);

        _reply.OnPressed += _ =>
        {
            if (_briefing != null)
                OnReply?.Invoke(_briefing.Channel, ReplyDraft(_briefing.Text));
        };
        _ticketLogs.OnPressed += _ => OnTicketLogs?.Invoke(SummaryId);
        _timeline.OnToggled += args =>
        {
            _timelineScroll.Visible = args.Pressed;
            if (!args.Pressed)
                return;

            _timelineBox.RemoveAllChildren();
            _timelineBox.AddChild(new Label
            {
                Text = Loc.GetString("admin-assist-timeline-loading"),
                StyleClasses = { "LabelSubText" },
            });
            OnTimeline?.Invoke(SummaryId);
        };
        _player.OnItemSelected += args =>
        {
            _player.SelectId(args.Id);
            UpdatePlayerButtons();
        };
        _follow.OnPressed += _ =>
        {
            if (SelectedPlayer() is { } player)
                OnFollow?.Invoke(player);
        };
        _note.OnPressed += _ =>
        {
            if (_briefing != null && SelectedPlayer() is { } player)
                OnNote?.Invoke(player, NoteDraft(_briefing));
        };
    }

    public void SetBriefing(AdminAssistBriefingEvent briefing, bool voted)
    {
        SummaryId = briefing.SummaryId;

        var title = Loc.GetString("admin-assist-briefing-title",
            ("time", briefing.CreatedAt.ToLocalTime().ToString("HH:mm")));
        if (briefing.Unreliable)
            title += " · " + Loc.GetString("admin-assist-briefing-unreliable");
        _title.Text = title;
        _title.FontColorOverride = briefing.Unreliable ? Color.Orange : null;

        SetVoted(voted);

        if (briefing.SummaryId != _briefing?.SummaryId)
        {
            _timeline.Pressed = false;
            _timelineScroll.Visible = false;
            _timelineBox.RemoveAllChildren();
        }

        var keep = _briefing?.Participants.SequenceEqual(briefing.Participants) ?? false;
        _briefing = briefing;
        if (!keep)
            _names = new Dictionary<string, NetUserId>();
        Render();
        _ticketLogs.Visible = CanLogs;
        _timeline.Visible = CanLogs;
        _reply.StyleClasses.Remove("OpenRight");
        _reply.StyleClasses.Remove("OpenBoth");
        _reply.StyleClasses.Add(CanLogs ? "OpenRight" : "OpenBoth");
        if (!keep)
        {
            _players.Clear();
            _player.Clear();
        }
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        if (float.IsFinite(availableSize.Y))
        {
            var body = Math.Clamp(availableSize.Y * BodyShare, MinScrollHeight, BodyMaxHeight);
            if (!MathHelper.CloseTo(_bodyScroll.MaxHeight, body))
                _bodyScroll.MaxHeight = body;

            var timeline = Math.Clamp(availableSize.Y * TimelineShare, MinScrollHeight, TimelineHeight);
            if (!MathHelper.CloseTo(_timelineScroll.SetHeight, timeline))
                _timelineScroll.SetHeight = timeline;
        }

        return base.MeasureOverride(availableSize);
    }

    public void SetPlayers(IReadOnlyList<PlayerInfo> playerList)
    {
        if (_briefing == null)
            return;

        var selected = SelectedPlayer()?.SessionId;
        var byId = playerList.ToDictionary(p => p.SessionId);
        _players.Clear();
        _player.Clear();
        var names = new Dictionary<string, NetUserId>();
        foreach (var user in _briefing.Participants)
        {
            if (!byId.TryGetValue(user, out var info))
                continue;

            AddName(names, info.Username, user);
            AddName(names, info.CharacterName, user);

            var label = info.CharacterName.Length > 0 ? $"{info.Username} ({info.CharacterName})" : info.Username;
            if (user == _briefing.Channel)
                label += " · " + Loc.GetString("admin-assist-briefing-author");
            _player.AddItem(label, _players.Count);
            if (info.SessionId == selected)
                _player.SelectId(_players.Count);
            _players.Add(info);
        }

        UpdatePlayerButtons();
        if (!names.OrderBy(n => n.Key).SequenceEqual(_names.OrderBy(n => n.Key)))
        {
            _names = names;
            Render();
        }
    }

    private static void AddName(Dictionary<string, NetUserId> names, string name, NetUserId user)
    {
        if (name.Length < MinNameLength)
            return;

        if (names.TryGetValue(name, out var other) && other != user)
            names[name] = default;
        else
            names[name] = user;
    }

    private void Render()
    {
        if (_briefing == null)
            return;

        var names = CanPlayerPanel
            ? _names.Where(n => n.Value != default).ToDictionary(n => n.Key, n => n.Value)
            : new Dictionary<string, NetUserId>();
        _body.SetMessage(
            Format(_briefing.SummaryId, _briefing.Text, _briefing.CitedLines, _briefing.CheckedCitations, names),
            AllowedTags);
    }

    public void SetTimeline(List<AdminAssistTimelineEntry> entries)
    {
        _timelineBox.RemoveAllChildren();
        if (entries.All(e => e.Kind != AdminAssistTimelineKind.Log))
        {
            _timelineBox.AddChild(new Label
            {
                Text = Loc.GetString("admin-assist-timeline-empty"),
                StyleClasses = { "LabelSubText" },
            });
        }

        foreach (var entry in entries)
        {
            var time = entry.Time.ToLocalTime().ToString("HH:mm:ss");
            var label = new Label { ClipText = true, HorizontalExpand = true };
            switch (entry.Kind)
            {
                case AdminAssistTimelineKind.TicketOpened:
                    label.Text = $"{time}  · {Loc.GetString("admin-assist-timeline-opened")} ·";
                    label.StyleClasses.Add("LabelSubText");
                    break;
                case AdminAssistTimelineKind.LastMessage:
                    label.Text = $"{time}  · {Loc.GetString("admin-assist-timeline-last-message")} ·";
                    label.StyleClasses.Add("LabelSubText");
                    break;
                default:
                    label.Text = $"{time}  {(entry.Cited ? "▶ " : "")}{entry.Text}";
                    label.ToolTip = entry.Text;
                    label.FontColorOverride = entry.Cited ? CitedColor : ImpactColor(entry.Impact);
                    break;
            }

            _timelineBox.AddChild(label);
        }
    }

    private static Color? ImpactColor(LogImpact impact)
    {
        return impact switch
        {
            LogImpact.Extreme => Color.Red,
            LogImpact.High => Color.Orange,
            _ => null,
        };
    }

    public void SetRequested(bool pending)
    {
        _request.Disabled = pending;
        _request.Text = Loc.GetString(pending ? "admin-assist-request-pending" : "admin-assist-request");
    }

    public void SetClaim(AdminAssistClaim? claim, NetUserId me)
    {
        _claimMine = claim != null && claim.Admin == me;
        _claimedBy.Visible = claim != null;
        if (claim != null)
            _claimedBy.Text = _claimMine
                ? Loc.GetString("admin-assist-claim-mine")
                : Loc.GetString("admin-assist-claim-by", ("admin", claim.AdminName));

        _claim.Text = Loc.GetString(claim == null
            ? "admin-assist-claim-take"
            : _claimMine ? "admin-assist-claim-release" : "admin-assist-claim-take-over");
    }

    private PlayerInfo? SelectedPlayer()
    {
        return _player.SelectedId >= 0 && _player.SelectedId < _players.Count ? _players[_player.SelectedId] : null;
    }

    private void UpdatePlayerButtons()
    {
        var player = SelectedPlayer();
        _player.Visible = _players.Count > 0 && (CanFollow || CanNotes);
        _follow.Visible = _player.Visible && CanFollow;
        _follow.Disabled = player?.NetEntity == null;
        _note.Visible = _player.Visible && CanNotes;
        _note.Disabled = player == null;
    }

    public static string ReplyDraft(string text)
    {
        var fields = Fields(text);
        var kind = fields.GetValueOrDefault("Тип", "").Trim().ToLowerInvariant();
        var rule = fields.GetValueOrDefault("Правила", "").Trim();

        if (kind.StartsWith("вопрос") && rule.Length > 0 && !rule.StartsWith("явно", StringComparison.OrdinalIgnoreCase))
            return Loc.GetString("admin-assist-reply-draft-question", ("rule", rule));
        if (kind.StartsWith("баг"))
            return Loc.GetString("admin-assist-reply-draft-bug");
        return Loc.GetString("admin-assist-reply-draft-default");
    }

    public static string NoteDraft(AdminAssistBriefingEvent briefing)
    {
        var gist = Fields(briefing.Text).GetValueOrDefault("Суть", "").Trim();
        return Loc.GetString("admin-assist-note-draft", ("round", briefing.RoundNumber), ("gist", gist));
    }

    private static Dictionary<string, string> Fields(string text)
    {
        var fields = new Dictionary<string, string>();
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var field = FieldRegex.Match(line);
            if (field.Success)
                fields.TryAdd(field.Groups[1].Value, line[field.Length..]);
        }

        return fields;
    }

    private void Vote(short vote)
    {
        SetVoted(true);
        OnVote?.Invoke(SummaryId, vote);
    }

    private void SetVoted(bool voted)
    {
        _up.Visible = !voted;
        _down.Visible = !voted;
        _voted.Visible = voted;
    }

    public static FormattedMessage Format(
        long summaryId,
        string text,
        IReadOnlyDictionary<string, string> citedLines,
        IReadOnlyCollection<string> checkedCitations,
        IReadOnlyDictionary<string, NetUserId> names)
    {
        var nameRegex = NameRegex(names.Keys);
        var msg = new FormattedMessage();
        var first = true;
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (!first)
                msg.PushNewline();
            first = false;

            var field = FieldRegex.Match(line);
            if (!field.Success)
            {
                AddSpans(msg, line, summaryId, citedLines, names, nameRegex);
                continue;
            }

            msg.PushTag(new MarkupNode("bold", null, null));
            msg.AddText(field.Value);
            msg.Pop();
            line = line[field.Length..];

            switch (field.Groups[1].Value)
            {
                case "Логи":
                    msg.AddText(" ");
                    AddMark(msg, HasChecked(line, checkedCitations) ? Mark.Confirmed : Mark.Unconfirmed);
                    AddSpans(msg, line.TrimStart(), summaryId, citedLines, names, nameRegex);
                    break;
                case "Расхождения":
                    var items = line.Split(';');
                    for (var i = 0; i < items.Length; i++)
                    {
                        if (i > 0)
                            msg.AddText(";");
                        var item = items[i];
                        var lead = item.Length - item.TrimStart().Length;
                        msg.AddText(item[..lead].Length > 0 ? item[..lead] : " ");
                        AddMark(msg, HasChecked(item, checkedCitations) ? Mark.Contradicted : Mark.Unconfirmed);
                        AddSpans(msg, item[lead..], summaryId, citedLines, names, nameRegex);
                    }
                    break;
                default:
                    AddSpans(msg, line, summaryId, citedLines, names, nameRegex);
                    break;
            }
        }

        return msg;
    }

    public enum Mark
    {
        Confirmed,
        Contradicted,
        Unconfirmed,
    }

    private static void AddMark(FormattedMessage msg, Mark mark)
    {
        var (icon, color) = mark switch
        {
            Mark.Confirmed => ("✔", Confirmed),
            Mark.Contradicted => ("✖", Contradicted),
            _ => ("?", Unconfirmed),
        };
        msg.PushColor(color);
        msg.AddText(icon + " ");
        msg.Pop();
    }

    private static bool HasChecked(string text, IReadOnlyCollection<string> checkedCitations)
    {
        return CiteRegex.Matches(text).Any(m => checkedCitations.Contains(m.Value));
    }

    private static Regex? NameRegex(IEnumerable<string> names)
    {
        var alternatives = names.OrderByDescending(n => n.Length).Select(Regex.Escape).ToList();
        return alternatives.Count == 0
            ? null
            : new Regex($@"(?<![\p{{L}}\p{{N}}_])(?:{string.Join("|", alternatives)})(?![\p{{L}}\p{{N}}_])");
    }

    private static void AddSpans(
        FormattedMessage msg,
        string line,
        long summaryId,
        IReadOnlyDictionary<string, string> citedLines,
        IReadOnlyDictionary<string, NetUserId> names,
        Regex? nameRegex)
    {
        var spans = CiteRegex.Matches(line)
            .Where(m => citedLines.ContainsKey(m.Value))
            .Select(m => (m.Index, m.Length, Cite: true))
            .Concat(nameRegex?.Matches(line).Select(m => (m.Index, m.Length, Cite: false)) ?? [])
            .OrderBy(span => span.Index)
            .ThenByDescending(span => span.Cite);

        var at = 0;
        foreach (var (index, length, cite) in spans)
        {
            if (index < at)
                continue;

            if (index > at)
                msg.AddText(line[at..index]);

            var value = line.Substring(index, length);
            if (cite)
            {
                msg.PushTag(new MarkupNode(AdminAssistCiteTag.TagName,
                        new MarkupParameter(value),
                        new Dictionary<string, MarkupParameter>
                        {
                            ["line"] = new(citedLines[value]),
                            ["summary"] = new(summaryId),
                        }),
                    selfClosing: true);
            }
            else
            {
                msg.PushTag(new MarkupNode(AdminAssistPlayerTag.TagName,
                        new MarkupParameter(value),
                        new Dictionary<string, MarkupParameter>
                        {
                            ["user"] = new(names[value].UserId.ToString()),
                        }),
                    selfClosing: true);
            }

            at = index + length;
        }

        if (at < line.Length)
            msg.AddText(line[at..]);
    }
}
