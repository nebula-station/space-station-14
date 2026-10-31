// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Client._Nebula.Squads.Cyber;
using Content.Client.Pinpointer.UI;
using Content.Client.UserInterface.Controls;
using Content.Shared._Nebula.Squads;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.Squads;

/// <summary>
/// Sensor map with squad members and orders on the left, collapsible squads on the right.
/// Clicking a squad plate selects it for orders, clicking a member centers the map on them.
/// </summary>
public sealed partial class SquadMonitorTab : BoxContainer
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private const float ListWidth = 280f;
    private static readonly ResPath BlipTexture = new("/Textures/Interface/NavMap/beveled_circle.png");

    public event Action<int, ProtoId<SquadOrderPrototype>, NetCoordinates>? PlaceOrderPressed;
    public event Action<int>? CancelOrderPressed;

    public readonly SquadNavMapControl Map;

    private readonly SpriteSystem _sprite;
    private readonly BoxContainer _squads;
    private readonly BoxContainer _hint;
    private readonly HashSet<int> _expanded = [];
    private SimpleRadialMenu? _menu;
    private SquadTabletState? _state;
    private int? _selected;

    public SquadMonitorTab()
    {
        IoCManager.InjectDependencies(this);
        _sprite = _entMan.System<SpriteSystem>();

        Orientation = LayoutOrientation.Horizontal;
        SeparationOverride = 10;
        VerticalExpand = true;

        Map = new SquadNavMapControl { HorizontalExpand = true, VerticalExpand = true };
        Map.MapRightClicked += OnMapRightClicked;
        Map.OrderRightClicked += id => CancelOrderPressed?.Invoke(id);

        var mapPanel = SquadUi.Section();
        mapPanel.Fill = Color.FromHex("#0b0204");
        mapPanel.AddChild(new ClipBox { HorizontalExpand = true, VerticalExpand = true, Children = { Map } });

        _squads = new BoxContainer { Orientation = LayoutOrientation.Vertical, SeparationOverride = 6 };
        _hint = new BoxContainer { Orientation = LayoutOrientation.Vertical, Margin = new Thickness(14, 4, 12, 12) };

        var column = SquadUi.Column(Loc.GetString("squad-tablet-squads"), _squads, width: ListWidth);
        column.AddChild(_hint);

        AddChild(mapPanel);
        AddChild(column);
        SetHint();
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        _menu?.Close();
    }

    public void SetGrid(EntityUid? grid)
    {
        Map.MapUid = grid;
        Map.ForceNavMapUpdate();
    }

    public void UpdateState(SquadTabletState state)
    {
        var same = SameCards(_state, state);
        _state = state;
        if (_selected is { } selected && !SquadUi.TryGetSquad(state, selected, out _))
            _selected = null;

        UpdateMap();
        if (!same)
            Rebuild();
    }

    /// <summary>
    /// Positions change on every refresh while anyone walks, the cards are rebuilt only when what they show changes,
    /// otherwise a button pressed between two refreshes would be replaced and lose the click.
    /// </summary>
    private static bool SameCards(SquadTabletState? old, SquadTabletState state)
    {
        if (old == null || !SquadUi.SameLayout(old, state))
            return false;

        for (var i = 0; i < state.Squads.Count; i++)
        {
            var oldMembers = old.Squads[i].Members;
            var members = state.Squads[i].Members;
            for (var j = 0; j < members.Count; j++)
            {
                if (oldMembers[j].Signal != members[j].Signal
                    || oldMembers[j].InCombat != members[j].InCombat
                    || oldMembers[j].Coordinates.HasValue != members[j].Coordinates.HasValue)
                    return false;
            }
        }

        return true;
    }

    private void SetHint()
    {
        _hint.RemoveAllChildren();
        var key = _selected == null ? "squad-tablet-monitor-hint" : "squad-tablet-monitor-hint-selected";
        _hint.AddChild(CyberDraw.Wrapped(Loc.GetString(key), CyberPalette.Cyan));
    }

    private void UpdateMap()
    {
        if (_state == null)
            return;

        var blip = _sprite.Frame0(new SpriteSpecifier.Texture(BlipTexture));
        Map.TrackedEntities.Clear();
        Map.Orders.Clear();
        foreach (var squad in _state.Squads)
        {
            foreach (var member in squad.Members)
            {
                if (member.Entity is not { } entity
                    || member.Coordinates is not { } coordinates)
                    continue;

                Map.TrackedEntities[entity] = new NavMapBlip(_entMan.GetCoordinates(coordinates),
                    blip,
                    squad.Color,
                    member.Signal == SquadMemberSignal.Dead || member.InCombat);
            }
        }

        foreach (var order in _state.Orders)
        {
            var coordinates = _entMan.GetCoordinates(order.Coordinates);
            if (coordinates.EntityId != Map.MapUid
                || !_proto.Resolve(order.Order, out var proto)
                || !SquadUi.TryGetSquad(_state, order.Squad, out var squad))
                continue;

            Map.Orders.Add(new SquadMapOrder(order.Id, coordinates.Position, _sprite.Frame0(proto.Icon), squad.Color));
        }
    }

    private void Rebuild()
    {
        _squads.RemoveAllChildren();
        if (_state == null)
            return;

        if (_state.Squads.Count == 0)
            _squads.AddChild(SquadUi.Muted(Loc.GetString("squad-tablet-no-squads")));

        foreach (var squad in _state.Squads)
        {
            _squads.AddChild(CreateSquad(squad));
        }

        SetHint();
    }

    private Control CreateSquad(SquadTabletSquad squad)
    {
        var expanded = _expanded.Contains(squad.Id);
        var card = new CyberPanel { Fill = CyberPalette.PanelRaised, Stripe = squad.Color, Cut = 0f, SeparationOverride = 1 };

        var header = new CyberButton(string.Empty, CyberButtonStyle.Row)
        {
            Accent = squad.Color,
            Active = _selected == squad.Id,
            HorizontalExpand = true,
            MinHeight = 32,
        };
        header.Label.Text = squad.Name.ToUpperInvariant();
        header.Label.FontOverride = CyberDraw.Font(12, true);
        header.Label.HorizontalExpand = true;
        header.Content.AddChild(CyberDraw.Label(Loc.GetString("squad-tablet-squad-count",
                ("alive", squad.Members.Count(m => m.Signal == SquadMemberSignal.Alive)),
                ("total", squad.Members.Count)),
            CyberPalette.Muted,
            11));
        header.OnPressed += _ =>
        {
            _selected = _selected == squad.Id ? null : squad.Id;
            Rebuild();
        };

        var expand = new CyberButton(expanded ? "-" : "+") { Accent = CyberPalette.Muted, MinWidth = 30 };
        expand.Content.Margin = new Thickness(6, 4);
        expand.OnPressed += _ =>
        {
            if (!_expanded.Remove(squad.Id))
                _expanded.Add(squad.Id);

            Rebuild();
        };

        card.AddChild(new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 4,
            Margin = new Thickness(3, 0, 4, 0),
            Children = { header, expand },
        });

        if (!expanded)
            return card;

        if (squad.Members.Count == 0)
            card.AddChild(SquadUi.Muted(Loc.GetString("squad-tablet-squad-empty")));

        foreach (var member in squad.Members)
        {
            card.AddChild(CreateMember(member));
        }

        return card;
    }

    private Control CreateMember(SquadTabletMember member)
    {
        var row = new CyberButton(member.Name, CyberButtonStyle.Row) { HorizontalExpand = true, MinHeight = 26 };
        row.Content.Margin = new Thickness(18, 3, 8, 3);
        row.Label.HorizontalExpand = true;
        row.Label.ClipText = true;
        row.Label.FontOverride = CyberDraw.Font(11);
        row.Content.AddChild(SquadUi.Status(member));

        var record = member.Record;
        if (member.Coordinates != null)
            row.OnPressed += _ => CenterOnMember(record);
        else
            row.Disabled = true;

        return row;
    }

    /// <summary>
    /// Looks the position up on press, the row outlives the state it was built from.
    /// </summary>
    private void CenterOnMember(uint record)
    {
        if (_state == null)
            return;

        foreach (var squad in _state.Squads)
        {
            var index = squad.Members.FindIndex(m => m.Record == record);
            if (index >= 0 && squad.Members[index].Coordinates is { } coordinates)
            {
                Map.CenterToCoordinates(_entMan.GetCoordinates(coordinates));
                return;
            }
        }
    }

    private void OnMapRightClicked(EntityCoordinates coordinates)
    {
        if (_state == null
            || _selected is not { } selected
            || !SquadUi.TryGetSquad(_state, selected, out var squad)
            || !_entMan.TryGetComponent<MapGridComponent>(coordinates.EntityId, out var grid)
            || !SquadOrders.IsOnStationTile(_entMan.System<SharedMapSystem>(), (coordinates.EntityId, grid), coordinates))
            return;

        var net = _entMan.GetNetCoordinates(coordinates);
        List<RadialMenuOptionBase> options = [];
        foreach (var order in _proto.EnumeratePrototypes<SquadOrderPrototype>().OrderBy(o => o.Order))
        {
            options.Add(new RadialMenuActionOption<ProtoId<SquadOrderPrototype>>(id => PlaceOrderPressed?.Invoke(selected, id, net), order.ID)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(order.Icon),
                ToolTip = Loc.GetString(order.Name),
                Color = squad.Color,
            });
        }

        _menu ??= new SimpleRadialMenu();
        _menu.SetButtons(options);
        _menu.OpenOverMouseScreenPosition();
    }

    /// <summary>
    /// Holds the map without asking for its fixed size and cuts off what does not fit,
    /// so the map never pushes the window or draws over the tabs.
    /// </summary>
    private sealed class ClipBox : Control
    {
        public ClipBox()
        {
            RectClipContent = true;
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            foreach (var child in Children)
            {
                child.Measure(availableSize);
            }

            return Vector2.Zero;
        }
    }
}
