// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Nebula.Squads.Cyber;
using Content.Shared._Nebula.Squads;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._Nebula.Squads;

/// <summary>
/// Pieces shared by the squad tablet tabs.
/// </summary>
public static class SquadUi
{
    public const float ColumnWidth = 240f;
    private const float DotSize = 8f;

    public static bool TryGetSquad(SquadTabletState state, int id, out SquadTabletSquad squad)
    {
        var index = state.Squads.FindIndex(s => s.Id == id);
        squad = index < 0 ? default : state.Squads[index];
        return index >= 0;
    }

    /// <summary>
    /// Same squads, members, free candidates and tasks. The sensor data that changes every refresh is left out,
    /// the roster and the task tabs show none of it, so they rebuild only when this says false.
    /// </summary>
    public static bool SameLayout(SquadTabletState? old, SquadTabletState state)
    {
        if (old == null
            || old.Squads.Count != state.Squads.Count
            || !SameMembers(old.Free, state.Free)
            || old.Tasks.Count != state.Tasks.Count)
            return false;

        for (var i = 0; i < state.Squads.Count; i++)
        {
            var a = old.Squads[i];
            var b = state.Squads[i];
            if (a.Id != b.Id
                || a.Name != b.Name
                || a.Color != b.Color
                || !SameMembers(a.Members, b.Members))
                return false;
        }

        for (var i = 0; i < state.Tasks.Count; i++)
        {
            var a = old.Tasks[i];
            var b = state.Tasks[i];
            if (a.Id != b.Id
                || a.Squad != b.Squad
                || a.Record != b.Record
                || a.Title != b.Title
                || a.Description != b.Description)
                return false;
        }

        return true;
    }

    private static bool SameMembers(List<SquadTabletMember> old, List<SquadTabletMember> members)
    {
        if (old.Count != members.Count)
            return false;

        for (var i = 0; i < members.Count; i++)
        {
            if (old[i].Record != members[i].Record
                || old[i].Name != members[i].Name
                || old[i].Job != members[i].Job)
                return false;
        }

        return true;
    }

    public static Control ColorDot(Color color)
    {
        return new CyberPanel
        {
            Fill = color,
            Stripe = Color.Transparent,
            Cut = 0f,
            SetSize = new Vector2(DotSize, DotSize),
            VerticalAlignment = Control.VAlignment.Center,
        };
    }

    public static Label Muted(string text)
    {
        var label = CyberDraw.Label(text, CyberPalette.Muted);
        label.Margin = new Thickness(4, 2);
        return label;
    }

    public static string StatusText(SquadTabletMember member)
    {
        return Loc.GetString(member.Signal switch
        {
            SquadMemberSignal.Alive => "squad-tablet-status-alive",
            SquadMemberSignal.Dead => "squad-tablet-status-dead",
            _ => "squad-tablet-status-none",
        });
    }

    /// <summary>
    /// State colors, none of them is used by a squad.
    /// </summary>
    public static Color StatusColor(SquadTabletMember member)
    {
        return member.Signal switch
        {
            SquadMemberSignal.Alive => CyberPalette.Cyan,
            SquadMemberSignal.Dead => CyberPalette.Red,
            _ => CyberPalette.Muted,
        };
    }

    /// <summary>
    /// Status of a member with the blinking combat mark in front while they are being hurt.
    /// </summary>
    public static Control Status(SquadTabletMember member)
    {
        var box = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 6 };
        if (member.InCombat)
            box.AddChild(new CombatMark());

        box.AddChild(CyberDraw.Label(StatusText(member), StatusColor(member), 11, true));
        return box;
    }

    /// <summary>
    /// Titled panel with a scrolling list, the columns of the tablet tabs.
    /// </summary>
    public static CyberPanel Column(string title, BoxContainer list, Control? header = null, float width = ColumnWidth)
    {
        var column = Section(width);
        var label = CyberDraw.Label(title.ToUpperInvariant(), CyberPalette.Red, 11, true);
        label.Margin = new Thickness(14, 10, 12, 6);
        column.AddChild(label);
        if (header != null)
        {
            header.Margin = new Thickness(12, 0, 10, 6);
            column.AddChild(header);
        }

        list.Margin = new Thickness(12, 2, 10, 10);
        column.AddChild(new ScrollContainer { VerticalExpand = true, HScrollEnabled = false, Children = { list } });
        return column;
    }

    /// <summary>
    /// Panel of a tab: fixed width when given, otherwise takes the rest of the row.
    /// </summary>
    public static CyberPanel Section(float width = 0f)
    {
        var panel = new CyberPanel { VerticalExpand = true };
        if (width > 0f)
        {
            panel.MinWidth = width;
            panel.MaxWidth = width;
        }
        else
            panel.HorizontalExpand = true;

        return panel;
    }

    /// <summary>
    /// Blinking "combat" plate shown while a member keeps taking damage.
    /// </summary>
    private sealed class CombatMark : PanelContainer
    {
        private const float Period = 0.5f;

        private static readonly StyleBoxFlat On = new()
        {
            BackgroundColor = CyberPalette.Text,
            BorderColor = CyberPalette.Text,
            BorderThickness = new Thickness(1),
        };

        private static readonly StyleBoxFlat Off = new()
        {
            BackgroundColor = Color.Transparent,
            BorderColor = CyberPalette.Text,
            BorderThickness = new Thickness(1),
        };

        private readonly Label _label;
        private double _time;

        public CombatMark()
        {
            _label = CyberDraw.Label(Loc.GetString("squad-tablet-combat"), CyberPalette.Text, 10, true);
            _label.Margin = new Thickness(5, 0);
            AddChild(_label);
            VerticalAlignment = VAlignment.Center;
            PanelOverride = On;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            _time += args.DeltaSeconds;
            var on = _time % Period < Period / 2f;
            PanelOverride = on ? On : Off;
            _label.FontColorOverride = on ? CyberPalette.Background : CyberPalette.Text;
        }
    }
}
