// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Client._Nebula.AntagTokens;
using Content.Client._Nebula.Discord.Linking;
using Content.Client._Nebula.Sponsor;
using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Client.Lobby;
using Content.Shared._Nebula.AntagTokens;
using Content.Shared._Nebula.CCVar;
using Content.Shared._Nebula.Sponsor;
using Content.Shared._Nebula.Wardrobe;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client._Nebula.Lobby.UI;

/// <summary>
/// Everything for the project's supporters in one window: the subscription status, its levels,
/// the antag token shop and what the wardrobe has for this level.
/// </summary>
public sealed partial class NebulaSupportWindow : NebulaWindow
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IEntitySystemManager _systems = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IStateManager _state = default!;
    [Dependency] private IUriOpener _uriOpener = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    private readonly SponsorSystem _sponsors;
    private readonly AntagTokenSystem _tokens;

    private readonly Label _rank = new() { HorizontalExpand = true };
    private readonly Label _balance = new();
    private readonly Label _weekly = new() { HorizontalExpand = true };
    private readonly Label _next = new();
    private readonly ShapedButton _linkButton;

    private readonly BoxContainer _tierList = new()
    {
        Orientation = BoxContainer.LayoutOrientation.Vertical,
        SeparationOverride = 6,
    };

    private readonly BoxContainer _wardrobeList = new()
    {
        Orientation = BoxContainer.LayoutOrientation.Vertical,
        SeparationOverride = 4,
    };

    private readonly AntagTokenPanel _tokenPanel = new();
    private readonly List<(ShapedButton Tab, Control Page)> _tabs = [];
    private bool _tabChosen;
    private bool _rankPagesBuilt;
    private int? _shownRank;

    public NebulaSupportWindow()
    {
        IoCManager.InjectDependencies(this);

        _sponsors = _systems.GetEntitySystem<SponsorSystem>();
        _tokens = _systems.GetEntitySystem<AntagTokenSystem>();

        Title = Loc.GetString("nebula-support-window-title");
        MinSize = new Vector2(620, 640);

        _rank.AddStyleClass(NebulaStyleClass.Title);
        _balance.AddStyleClass(NebulaStyleClass.Title);
        _weekly.AddStyleClass(NebulaStyleClass.TextMuted);
        _next.AddStyleClass(NebulaStyleClass.TextMuted);

        _linkButton = CreateButton("nebula-support-link-discord", NebulaStyleClass.ButtonFilled);
        _linkButton.OnPressed += _ => _ui.GetUIController<DiscordLinkingUIController>().ToggleWindow();

        Contents.AddChild(CreateHeader());

        var tabBar = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
        };
        Contents.AddChild(tabBar);

        var pages = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
        };
        Contents.AddChild(pages);

        AddTab(tabBar, pages, "nebula-support-tab-tiers", CreateTiersPage());
        AddTab(tabBar, pages, "nebula-support-tab-tokens", _tokenPanel);
        AddTab(tabBar, pages, "nebula-support-tab-wardrobe", CreateWardrobePage());

        _tokenPanel.BuyPressed += listing => _tokens.Buy(listing);
        _tokenPanel.CancelPressed += () => _tokens.Cancel();

        SelectTab(0);

        _sponsors.StatusChanged += Refresh;
        _tokens.StateChanged += Refresh;
        _tokens.RunLevelChanged += _tokens.RequestState;
        OnClose += () =>
        {
            _sponsors.StatusChanged -= Refresh;
            _tokens.StateChanged -= Refresh;
            _tokens.RunLevelChanged -= _tokens.RequestState;
        };

        Refresh();
        _sponsors.RequestStatus();
        _tokens.RequestState();
    }

    private Control CreateHeader()
    {
        var header = new ShapedPanel { Orientation = BoxContainer.LayoutOrientation.Vertical };
        header.AddStyleClass(NebulaStyleClass.Section);
        header.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(14, 10),
            SeparationOverride = 2,
            Children =
            {
                new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    Children = { _rank, _balance },
                },
                new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    Children = { _weekly, _next },
                },
                _linkButton,
            },
        });
        return header;
    }

    private static ShapedButton CreateButton(string text, string style)
    {
        var button = new ShapedButton { Text = Loc.GetString(text) };
        button.AddStyleClass(style);
        return button;
    }

    private void AddTab(BoxContainer tabBar, BoxContainer pages, string text, Control page)
    {
        var tab = new ShapedButton { Text = Loc.GetString(text), ToggleMode = true };
        tab.AddStyleClass(NebulaStyleClass.ButtonTonal);

        var index = _tabs.Count;
        tab.OnPressed += _ =>
        {
            _tabChosen = true;
            SelectTab(index);
        };

        tabBar.AddChild(tab);
        pages.AddChild(page);
        _tabs.Add((tab, page));
    }

    private void SelectTab(int index)
    {
        for (var i = 0; i < _tabs.Count; i++)
        {
            _tabs[i].Tab.Pressed = i == index;
            _tabs[i].Page.Visible = i == index;
        }
    }

    private Control CreateTiersPage()
    {
        var page = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
            SeparationOverride = 6,
        };

        page.AddChild(new ScrollContainer
        {
            VerticalExpand = true,
            HScrollEnabled = false,
            Children = { _tierList },
        });

        var link = _cfg.GetCVar(NCCVars.InfoLinksBoosty);
        if (link != string.Empty)
        {
            var boosty = CreateButton("nebula-support-open-boosty", NebulaStyleClass.ButtonSupport);
            boosty.OnPressed += _ => _uriOpener.OpenUri(link);
            page.AddChild(boosty);
        }

        return page;
    }

    private Control CreateWardrobePage()
    {
        var hint = new Label { Text = Loc.GetString("nebula-support-wardrobe-hint") };
        hint.AddStyleClass(NebulaStyleClass.TextMuted);

        var open = CreateButton("nebula-support-open-wardrobe", NebulaStyleClass.ButtonFilled);
        open.OnPressed += _ =>
        {
            if (_state.CurrentState is not LobbyState lobby)
                return;

            Close();
            lobby.OpenWardrobe();
        };

        return new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
            SeparationOverride = 6,
            Children =
            {
                hint,
                new ScrollContainer
                {
                    VerticalExpand = true,
                    HScrollEnabled = false,
                    Children = { _wardrobeList },
                },
                open,
            },
        };
    }

    private void Refresh()
    {
        var rank = _sponsors.Rank;
        var state = _tokens.State;

        _rank.Text = rank is { } level
            ? Loc.GetString("nebula-support-rank", ("level", level))
            : Loc.GetString("nebula-support-no-rank");

        UpdateTokens(state);
        if (!_rankPagesBuilt || _shownRank != rank)
        {
            _rankPagesBuilt = true;
            _shownRank = rank;
            UpdateTiers(rank);
            UpdateWardrobe(rank);
        }

        if (state == null)
            return;

        _tokenPanel.UpdateState(state);

        // Subscribers come here for the tokens, everyone else first needs to see the levels.
        if (!_tabChosen && (rank != null || state.Balance > 0))
            SelectTab(1);
    }

    private void UpdateTokens(AntagTokenStateMessage? state)
    {
        _balance.Text = Loc.GetString("antag-tokens-balance", ("balance", state?.Balance ?? 0));
        _linkButton.Visible = state is { Available: true, Linked: false };

        var weekly = state?.WeeklyTokens ?? 0;
        _weekly.Visible = weekly > 0;
        _next.Visible = weekly > 0;

        if (state == null || weekly <= 0)
            return;

        _weekly.Text = Loc.GetString("nebula-support-weekly-tokens", ("amount", weekly));
        _next.Text = NextGrantText(state);
    }

    private static string NextGrantText(AntagTokenStateMessage state)
    {
        if (state.Balance >= state.WeeklyTokenCap)
            return Loc.GetString("nebula-support-cap-reached");

        if (state.NextWeeklyGrantIn is not { } next || next <= TimeSpan.Zero)
            return Loc.GetString("nebula-support-next-soon");

        return next.TotalDays >= 1
            ? Loc.GetString("nebula-support-next-days", ("days", (int) Math.Ceiling(next.TotalDays)))
            : Loc.GetString("nebula-support-next-hours", ("hours", (int) Math.Ceiling(next.TotalHours)));
    }

    private void UpdateTiers(int? rank)
    {
        _tierList.RemoveAllChildren();

        foreach (var tier in _proto.EnumeratePrototypes<SponsorTierPrototype>().OrderBy(t => t.Rank))
        {
            _tierList.AddChild(CreateTierCard(tier, tier.Rank == rank));
        }
    }

    private static Control CreateTierCard(SponsorTierPrototype tier, bool current)
    {
        var name = new Label { Text = Loc.GetString(tier.Name), HorizontalExpand = true };
        name.AddStyleClass(NebulaStyleClass.Title);

        var title = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Children = { name },
        };

        if (current)
        {
            var yours = new Label { Text = Loc.GetString("nebula-support-your-tier") };
            yours.AddStyleClass(NebulaStyleClass.TextValue);
            title.AddChild(yours);
        }

        var body = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(14, 10),
            SeparationOverride = 2,
            Children = { title },
        };

        foreach (var perk in tier.Perks)
        {
            var text = new RichTextLabel { HorizontalExpand = true };
            text.SetMessage($"• {Loc.GetString(perk)}");
            body.AddChild(text);
        }

        var card = new ShapedPanel { Orientation = BoxContainer.LayoutOrientation.Vertical };
        card.AddStyleClass(NebulaStyleClass.Section);
        card.AddChild(body);
        return card;
    }

    private void UpdateWardrobe(int? rank)
    {
        _wardrobeList.RemoveAllChildren();

        foreach (var category in _proto.EnumeratePrototypes<WardrobeCategoryPrototype>().OrderBy(c => c.Order))
        {
            var total = category.Sets.Sum(set => set.Items.Count);
            var allowed = category.Sets.Where(set => set.IsAllowed(rank)).Sum(set => set.Items.Count);

            var row = new Label
            {
                Text = Loc.GetString("nebula-support-wardrobe-category",
                    ("name", Loc.GetString(category.Name)),
                    ("allowed", allowed),
                    ("total", total)),
            };
            row.AddStyleClass(NebulaStyleClass.TextValue);
            _wardrobeList.AddChild(row);
        }
    }
}
