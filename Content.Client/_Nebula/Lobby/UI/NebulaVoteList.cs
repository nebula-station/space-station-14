// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client.Voting;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client._Nebula.Lobby.UI;

/// <summary>
/// Shows a <see cref="NebulaVotePopup"/> for every running vote the player hasn't hidden, and can be dragged around
/// by any popup. Syncs with <see cref="VoteManager.ActiveVotes"/> on <see cref="VoteManager.ActiveVotesChanged"/>.
/// </summary>
public sealed partial class NebulaVoteList : BoxContainer
{
    [Dependency] private IVoteManager _voteManager = default!;

    private const float PopupWidth = 380f;

    private readonly Dictionary<int, NebulaVotePopup> _popups = new();
    private readonly HashSet<int> _activeIds = [];
    private readonly HashSet<int> _dismissed = [];

    private bool _dragging;
    private Vector2 _dragOffset;

    public NebulaVoteList()
    {
        IoCManager.InjectDependencies(this);

        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 6;
        MouseFilter = MouseFilterMode.Stop;
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        if (_voteManager is not VoteManager manager)
            return;

        manager.ActiveVotesChanged += Sync;
        Sync();
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        if (_voteManager is VoteManager manager)
            manager.ActiveVotesChanged -= Sync;
    }

    private void Sync()
    {
        if (_voteManager is not VoteManager manager)
            return;

        _activeIds.Clear();

        foreach (var vote in manager.ActiveVotes)
        {
            _activeIds.Add(vote.Id);

            if (_popups.ContainsKey(vote.Id) || _dismissed.Contains(vote.Id))
                continue;

            var id = vote.Id;
            var popup = new NebulaVotePopup(vote) { SetWidth = PopupWidth };
            popup.OnDismissed += () => Dismiss(id);
            _popups.Add(id, popup);
            AddChild(popup);
        }

        _dismissed.IntersectWith(_activeIds);

        if (_popups.Count + _dismissed.Count == _activeIds.Count)
            return;

        foreach (var (id, popup) in _popups)
        {
            if (_activeIds.Contains(id))
                continue;

            popup.Orphan();
            _popups.Remove(id);
        }
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _dragging = true;
        _dragOffset = args.RelativePosition;
        args.Handle();
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function == EngineKeyFunctions.UIClick)
            _dragging = false;
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (!_dragging || Parent is not { } parent)
            return;

        var position = args.GlobalPosition - parent.GlobalPosition - _dragOffset;
        var max = Vector2.Max(Vector2.Zero, parent.Size - Size);
        LayoutContainer.SetPosition(this, Vector2.Clamp(position, Vector2.Zero, max));
    }

    private void Dismiss(int id)
    {
        if (!_popups.Remove(id, out var popup))
            return;

        popup.Orphan();
        _dismissed.Add(id);
    }
}
