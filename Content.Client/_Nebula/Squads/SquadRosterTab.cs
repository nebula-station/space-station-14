// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Nebula.Squads.Cyber;
using Content.Client._Nebula.UserInterface;
using Content.Shared._Nebula.Squads;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._Nebula.Squads;

/// <summary>
/// Free security members and squads side by side, members are dragged between them. The right panel creates
/// a squad, with a box that collects the members it starts with, or shows the chosen squad with a box to drop into.
/// </summary>
public sealed class SquadRosterTab : BoxContainer
{
    private const float SwatchSize = 28f;
    private const float DropBoxHeight = 190f;

    public event Action<string, int, List<uint>>? CreateSquadPressed;
    public event Action<int>? DeleteSquadPressed;
    public event Action<uint, int?>? AssignPressed;
    public event Action<int, uint>? SetLeaderPressed;

    private readonly BoxContainer _free;
    private readonly BoxContainer _squads;
    private readonly BoxContainer _panel;
    private readonly CyberPanel _freeColumn;
    private readonly BoxContainer _createForm;
    private readonly LineEdit _nameEdit;
    private readonly BoxContainer _swatches;
    private readonly BoxContainer _pendingBox;
    private readonly CyberButton _create;
    private readonly NebulaDragDrop<RosterDrag> _drag = new(CyberGhost.Create);

    /// <summary>
    /// Members dropped into the squad being created, sent along when it is created.
    /// </summary>
    private readonly List<SquadTabletMember> _pending = [];

    /// <summary>
    /// Pending members taken out of a squad, kept in the box until the state shows them free.
    /// </summary>
    private readonly HashSet<uint> _releasing = [];
    private SquadTabletState? _state;

    /// <summary>
    /// Squad shown in the right panel, null shows the creation form.
    /// </summary>
    private int? _shown;
    private string? _pendingName;
    private string _name = string.Empty;
    private int _color = -1;

    private readonly record struct RosterDrag(SquadTabletMember Member, int? Squad, bool Pending);

    public SquadRosterTab()
    {
        Orientation = LayoutOrientation.Horizontal;
        SeparationOverride = 10;
        VerticalExpand = true;

        _free = new BoxContainer { Orientation = LayoutOrientation.Vertical, SeparationOverride = 3 };
        _squads = new BoxContainer { Orientation = LayoutOrientation.Vertical, SeparationOverride = 6 };
        _panel = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 10,
            Margin = new Thickness(16, 12, 14, 14),
            VerticalExpand = true,
        };

        var newSquad = new CyberButton(Loc.GetString("squad-tablet-new-squad")) { Accent = CyberPalette.Red };
        newSquad.OnPressed += _ =>
        {
            _shown = null;
            RebuildPanel();
        };

        _freeColumn = SquadUi.Column(Loc.GetString("squad-tablet-free"), _free);
        var panel = SquadUi.Section();
        panel.AddChild(_panel);

        AddChild(_freeColumn);
        AddChild(SquadUi.Column(Loc.GetString("squad-tablet-squads"), _squads, newSquad, 320f));
        AddChild(panel);

        _nameEdit = CyberDraw.Input(new LineEdit
        {
            PlaceHolder = Loc.GetString("squad-tablet-name-placeholder"),
            HorizontalExpand = true,
            IsValid = text => text.Length <= SquadLimits.MaxNameLength,
        });
        _nameEdit.OnTextChanged += args =>
        {
            _name = args.Text;
            UpdateCreate();
        };

        _swatches = new BoxContainer { Orientation = LayoutOrientation.Horizontal, SeparationOverride = 6 };
        _pendingBox = new BoxContainer { Orientation = LayoutOrientation.Vertical };
        _create = new CyberButton(Loc.GetString("squad-tablet-create"))
        {
            Accent = CyberPalette.Red,
            HorizontalAlignment = HAlignment.Right,
        };
        _create.OnPressed += _ => CreateSquad();

        _createForm = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 10,
            VerticalExpand = true,
            Children =
            {
                CyberDraw.Label(Loc.GetString("squad-tablet-create-title").ToUpperInvariant(), CyberPalette.Red, 12, true),
                _nameEdit,
                _swatches,
                _pendingBox,
                _create,
            },
        };

        _drag.Finished += Rebuild;
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        _drag.Cancel();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        _drag.FrameUpdate();
    }

    public void UpdateState(SquadTabletState state)
    {
        var same = SquadUi.SameLayout(_state, state);
        _state = state;
        if (same)
            return;

        var created = state.Squads.FindIndex(s => s.Name == _pendingName);
        if (_pendingName != null && created >= 0)
        {
            _shown = state.Squads[created].Id;
            _pendingName = null;
        }

        if (_shown is { } shown && !SquadUi.TryGetSquad(state, shown, out _))
            _shown = null;

        _pending.RemoveAll(p => !IsPendingKept(state, p.Record));
        if (!_drag.Dragging)
            Rebuild();
    }

    #region Build

    private void Rebuild()
    {
        if (_state == null)
            return;

        _drag.ClearZones();
        _drag.AddZone(_freeColumn, DropToFree);

        _free.RemoveAllChildren();
        var free = 0;
        foreach (var member in _state.Free)
        {
            if (_pending.Exists(p => p.Record == member.Record))
                continue;

            _free.AddChild(CreateMember(member, null, false, false));
            free++;
        }

        if (free == 0)
            _free.AddChild(SquadUi.Muted(Loc.GetString("squad-tablet-free-empty")));

        _squads.RemoveAllChildren();
        if (_state.Squads.Count == 0)
            _squads.AddChild(SquadUi.Muted(Loc.GetString("squad-tablet-no-squads")));

        foreach (var squad in _state.Squads)
        {
            _squads.AddChild(CreateSquad(squad));
        }

        RebuildPanel();
    }

    private Control CreateSquad(SquadTabletSquad squad)
    {
        var card = new CyberPanel { Fill = CyberPalette.PanelRaised, Stripe = squad.Color, Cut = 0f, SeparationOverride = 2 };
        var id = squad.Id;
        _drag.AddZone(card, item => DropToSquad(item, id));

        var header = new CyberButton(string.Empty, CyberButtonStyle.Row)
        {
            Accent = squad.Color,
            Active = _shown == squad.Id,
            MinHeight = 30,
        };
        header.Label.Text = squad.Name.ToUpperInvariant();
        header.Label.FontOverride = CyberDraw.Font(12, true);
        header.Label.HorizontalExpand = true;
        header.Content.AddChild(CyberDraw.Label(squad.Members.Count.ToString(), CyberPalette.Muted, 11));
        header.OnPressed += _ =>
        {
            _shown = id;
            Rebuild();
        };
        card.AddChild(header);

        for (var i = 0; i < squad.Members.Count; i++)
        {
            card.AddChild(CreateMember(squad.Members[i], squad.Id, false, i == 0));
        }

        return card;
    }

    private void RebuildPanel()
    {
        if (_state == null)
        {
            _panel.RemoveAllChildren();
            return;
        }

        if (_shown is { } shown && SquadUi.TryGetSquad(_state, shown, out var squad))
        {
            _panel.RemoveAllChildren();
            BuildSquadPanel(squad);
            return;
        }

        // The form stays in the tree so a state arriving mid-typing keeps the name edit and its focus.
        if (_createForm.Parent != _panel)
        {
            _panel.RemoveAllChildren();
            _panel.AddChild(_createForm);
        }

        RebuildSwatches();
        _pendingBox.RemoveAllChildren();
        var box = CreateDropBox(_pending, null, true);
        _drag.AddZone(box, DropToNew);
        _pendingBox.AddChild(box);
        UpdateCreate();
    }

    private void RebuildSwatches()
    {
        _swatches.RemoveAllChildren();
        for (var i = 0; i < SquadPalette.Colors.Length; i++)
        {
            var index = i;
            var color = SquadPalette.Colors[i];
            var swatch = new CyberButton(string.Empty, CyberButtonStyle.Swatch)
            {
                Swatch = color,
                Active = _color == i,
                Disabled = _state!.Squads.Exists(s => s.Color == color),
                SetSize = new Vector2(SwatchSize, SwatchSize),
            };
            swatch.OnPressed += _ =>
            {
                _color = index;
                RebuildSwatches();
                UpdateCreate();
            };
            _swatches.AddChild(swatch);
        }
    }

    private void UpdateCreate()
    {
        _create.Disabled = _state == null
            || _name.Trim().Length == 0
            || _color < 0
            || _state.Squads.Count >= SquadLimits.MaxSquads
            || _state.Squads.Exists(s => s.Name == _name.Trim() || s.Color == SquadPalette.Colors[_color]);
    }

    private void CreateSquad()
    {
        if (_create.Disabled)
            return;

        _pendingName = _name.Trim();
        List<uint> members = new(_pending.Count);
        foreach (var member in _pending)
        {
            members.Add(member.Record);
        }

        CreateSquadPressed?.Invoke(_pendingName, _color, members);
        _pending.Clear();
        _releasing.Clear();
        _name = string.Empty;
        _nameEdit.Text = string.Empty;
        _color = -1;
        RebuildPanel();
    }

    private void BuildSquadPanel(SquadTabletSquad squad)
    {
        var title = CyberDraw.Label(squad.Name.ToUpperInvariant(), CyberPalette.Text, 14, true);
        title.HorizontalExpand = true;
        title.ClipText = true;
        title.VerticalAlignment = VAlignment.Center;

        var id = squad.Id;
        var delete = new CyberButton(Loc.GetString("squad-tablet-delete")) { Accent = CyberPalette.Red };
        delete.OnPressed += _ => DeleteSquadPressed?.Invoke(id);

        var box = CreateDropBox(squad.Members, squad.Id, false);
        _drag.AddZone(box, item => DropToSquad(item, id));

        _panel.AddChild(new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 10,
            Children = { SquadUi.ColorDot(squad.Color), title, delete },
        });
        _panel.AddChild(box);
    }

    private CyberPanel CreateDropBox(List<SquadTabletMember> members, int? squad, bool pending)
    {
        var box = new CyberPanel
        {
            Fill = CyberPalette.Input,
            Border = CyberPalette.CyanDim,
            Stripe = Color.Transparent,
            Cut = 8f,
            MinHeight = DropBoxHeight,
            VerticalExpand = !pending,
            SeparationOverride = 3,
        };
        var list = new BoxContainer { Orientation = LayoutOrientation.Vertical, SeparationOverride = 3, Margin = new Thickness(8) };
        if (members.Count == 0)
        {
            var hint = CyberDraw.Label(Loc.GetString("squad-tablet-drop-hint").ToUpperInvariant(), CyberPalette.CyanDim, 12, true);
            hint.HorizontalAlignment = HAlignment.Center;
            hint.Margin = new Thickness(0, 60, 0, 0);
            list.AddChild(hint);
        }

        for (var i = 0; i < members.Count; i++)
        {
            list.AddChild(CreateMember(members[i], squad, pending, i == 0));
        }

        box.AddChild(list);
        return box;
    }

    /// <param name="first">The first member of a squad leads it, the others get a button to take the lead.</param>
    private Control CreateMember(SquadTabletMember member, int? squad, bool pending, bool first)
    {
        var row = new CyberButton(member.Name, CyberButtonStyle.Row) { MinHeight = 28 };
        row.Label.HorizontalExpand = true;
        row.Label.ClipText = true;
        if (squad is { } id && !pending)
            row.Content.AddChild(first ? CreateLeaderTag() : CreateLeaderButton(id, member.Record));

        row.Content.AddChild(CyberDraw.Label(member.Job, CyberPalette.Muted, 11));
        _drag.Attach(row, new RosterDrag(member, squad, pending), member.Name);
        return row;
    }

    private static Control CreateLeaderTag()
    {
        var tag = CyberDraw.Label(Loc.GetString("squad-tablet-leader").ToUpperInvariant(), CyberPalette.Cyan, 11, true);
        tag.VerticalAlignment = VAlignment.Center;
        return tag;
    }

    private Control CreateLeaderButton(int squad, uint record)
    {
        var button = new CyberButton(Loc.GetString("squad-tablet-make-leader").ToUpperInvariant())
        {
            ToolTip = Loc.GetString("squad-tablet-make-leader-tooltip"),
            VerticalAlignment = VAlignment.Center,
        };
        button.Content.Margin = new Thickness(6, 1);
        button.Label.FontOverride = CyberDraw.Font(10, true);
        button.OnPressed += _ => SetLeaderPressed?.Invoke(squad, record);
        return button;
    }

    #endregion

    #region Drag

    private void DropToFree(RosterDrag item)
    {
        if (item.Pending)
            RemovePending(item.Member.Record);
        else if (item.Squad != null)
            AssignPressed?.Invoke(item.Member.Record, null);
    }

    /// <summary>
    /// A member of a squad dropped here leaves it right away and waits in the box as free.
    /// </summary>
    private void DropToNew(RosterDrag item)
    {
        if (item.Pending)
            return;

        if (item.Squad != null)
        {
            _releasing.Add(item.Member.Record);
            AssignPressed?.Invoke(item.Member.Record, null);
        }

        _pending.Add(item.Member);
    }

    private void DropToSquad(RosterDrag item, int squad)
    {
        if (item.Squad == squad)
            return;

        if (item.Pending)
            RemovePending(item.Member.Record);

        AssignPressed?.Invoke(item.Member.Record, squad);
    }

    private void RemovePending(uint record)
    {
        _pending.RemoveAll(p => p.Record == record);
        _releasing.Remove(record);
    }

    private bool IsPendingKept(SquadTabletState state, uint record)
    {
        if (state.Free.Exists(f => f.Record == record))
        {
            _releasing.Remove(record);
            return true;
        }

        if (_releasing.Contains(record) && state.Squads.Exists(s => s.Members.Exists(m => m.Record == record)))
            return true;

        _releasing.Remove(record);
        return false;
    }

    #endregion
}
