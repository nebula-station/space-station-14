// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Client._Nebula.UserInterface;
using Content.Client.Voting;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._Nebula.Lobby.UI;

/// <summary>
/// Nebula card for a running vote: caller, title, one button per option and the time left.
/// Reads the shared <see cref="VoteManager.ActiveVote"/> every frame, since vote data is updated in place.
/// </summary>
public sealed partial class NebulaVotePopup : Control
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IVoteManager _voteManager = default!;

    private const int MaxColumns = 3;

    private readonly VoteManager.ActiveVote _vote;
    private readonly Label _caller;
    private readonly Label _title;
    private readonly ShapedButton[] _options;
    private readonly NebulaProgressBar _progress;
    private readonly Label _timeLeft;

    private readonly int[] _shownVotes;
    private int? _shownOurVote;
    private int _shownSecondsLeft = -1;
    private bool _dirty = true;

    /// <summary>
    /// Raised when the player hides this vote, the vote itself keeps running.
    /// </summary>
    public event Action? OnDismissed;

    public int VoteId => _vote.Id;

    public NebulaVotePopup(VoteManager.ActiveVote vote)
    {
        IoCManager.InjectDependencies(this);
        _vote = vote;

        var panel = new ShapedPanel { Orientation = BoxContainer.LayoutOrientation.Vertical };
        panel.AddStyleClass(NebulaStyleClass.Popup);
        AddChild(panel);

        var content = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(12, 10),
            SeparationOverride = 8,
        };
        panel.AddChild(content);

        var header = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
        };

        var icon = new TextureRect
        {
            Texture = NebulaIcons.Get("how_to_vote"),
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            SetSize = new Vector2(16, 16),
            VerticalAlignment = VAlignment.Center,
        };
        icon.AddStyleClass(NebulaStyleClass.Icon);

        _caller = new Label { ClipText = true, HorizontalExpand = true };
        _caller.AddStyleClass(NebulaStyleClass.TextMuted);

        var dismiss = new ShapedButton
        {
            Icon = NebulaIcons.Get("close"),
            IconSize = 14,
            MinWidth = 22,
            MinHeight = 22,
            VerticalAlignment = VAlignment.Center,
            ToolTip = Loc.GetString("nebula-lobby-vote-dismiss"),
        };
        dismiss.AddStyleClass(NebulaStyleClass.ButtonGhost);
        dismiss.OnPressed += _ => OnDismissed?.Invoke();

        header.AddChild(icon);
        header.AddChild(_caller);
        header.AddChild(dismiss);
        content.AddChild(header);

        _title = new Label();
        _title.AddStyleClass(NebulaStyleClass.Title);
        content.AddChild(_title);

        var grid = new GridContainer
        {
            Columns = Math.Clamp(vote.Entries.Length, 1, MaxColumns),
            HSeparationOverride = 6,
            VSeparationOverride = 6,
        };

        _options = new ShapedButton[vote.Entries.Length];
        _shownVotes = new int[vote.Entries.Length];

        for (var i = 0; i < _options.Length; i++)
        {
            var option = i;
            var button = new ShapedButton { HorizontalExpand = true };
            button.AddStyleClass(NebulaStyleClass.ButtonTonal);
            button.OnPressed += _ => _voteManager.SendCastVote(_vote.Id, option);

            _options[i] = button;
            grid.AddChild(button);
        }

        content.AddChild(grid);

        var footer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
        };

        _progress = new NebulaProgressBar
        {
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
        };

        _timeLeft = new Label { MinWidth = 36, Align = Label.AlignMode.Right };
        _timeLeft.AddStyleClass(NebulaStyleClass.TextMuted);

        footer.AddChild(_progress);
        footer.AddChild(_timeLeft);
        content.AddChild(footer);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        UpdateTime();

        if (!_dirty && !HasChanged())
            return;

        _dirty = false;
        _caller.Text = Loc.GetString("ui-vote-created", ("initiator", _vote.Initiator));
        _title.Text = _vote.Title;
        _shownOurVote = _vote.OurVote;

        for (var i = 0; i < _options.Length; i++)
        {
            var entry = _vote.Entries[i];
            _shownVotes[i] = entry.Votes;
            _options[i].IsActive = _vote.OurVote == i;
            _options[i].Text = _vote.DisplayVotes
                ? Loc.GetString("ui-vote-button", ("text", entry.Text), ("votes", entry.Votes))
                : Loc.GetString("ui-vote-button-no-votes", ("text", entry.Text));
        }
    }

    private bool HasChanged()
    {
        if (_vote.OurVote != _shownOurVote || _title.Text != _vote.Title)
            return true;

        for (var i = 0; i < _shownVotes.Length; i++)
        {
            if (_vote.Entries[i].Votes != _shownVotes[i])
                return true;
        }

        return false;
    }

    private void UpdateTime()
    {
        var now = _timing.RealTime;
        var total = (_vote.EndTime - _vote.StartTime).TotalSeconds;
        var left = _vote.EndTime - now;

        if (left < TimeSpan.Zero)
            left = TimeSpan.Zero;

        var seconds = (int) Math.Ceiling(left.TotalSeconds);
        var progress = total <= 0 ? 1f : (float) ((now - _vote.StartTime).TotalSeconds / total);

        if (MathF.Abs(progress - _progress.Value) * _progress.PixelWidth >= 1f)
            _progress.Value = progress;

        if (seconds == _shownSecondsLeft)
            return;

        _shownSecondsLeft = seconds;
        _timeLeft.Text = $"{TimeSpan.FromSeconds(seconds):m\\:ss}";
    }
}
