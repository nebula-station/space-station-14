// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Shared._Nebula.AntagTokens;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._Nebula.AntagTokens;

/// <summary>
/// Shows what can be bought with antag tokens right now and why the rest is closed.
/// </summary>
public sealed partial class AntagTokenPanel : BoxContainer
{
    [Dependency] private IPrototypeManager _proto = default!;

    private readonly RichTextLabel _status = new() { HorizontalExpand = true };
    private readonly BoxContainer _listings = new()
    {
        Orientation = LayoutOrientation.Vertical,
        SeparationOverride = 6,
    };

    public event Action<ProtoId<AntagTokenListingPrototype>>? BuyPressed;
    public event Action? CancelPressed;

    public AntagTokenPanel()
    {
        IoCManager.InjectDependencies(this);

        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 6;
        VerticalExpand = true;

        var description = new RichTextLabel { HorizontalExpand = true };
        description.SetMessage(Loc.GetString("antag-tokens-description"));

        AddChild(description);
        AddChild(_status);
        AddChild(new ScrollContainer
        {
            VerticalExpand = true,
            HScrollEnabled = false,
            Children = { _listings },
        });

        _status.SetMessage(Loc.GetString("antag-tokens-loading"));
    }

    public void UpdateState(AntagTokenStateMessage state)
    {
        _status.SetMessage(StatusText(state));

        _listings.RemoveAllChildren();

        var canBuy = state.Available && state.Linked && state.InLobby && state.ActiveListing == null
            && state.Balance >= 1;

        foreach (var listing in state.Listings)
        {
            _listings.AddChild(CreateRow(state, listing, canBuy));
        }
    }

    private string StatusText(AntagTokenStateMessage state)
    {
        var error = state.Error switch
        {
            AntagTokenError.InsufficientTokens => "antag-tokens-error-insufficient",
            AntagTokenError.ActivePurchase => "antag-tokens-error-active",
            AntagTokenError.NotEnoughPlayers => "antag-tokens-error-players",
            AntagTokenError.Busy => "antag-tokens-error-busy",
            AntagTokenError.NotInLobby => "antag-tokens-lobby-only",
            AntagTokenError.NotLinked => "antag-tokens-not-linked",
            AntagTokenError.Unavailable => "antag-tokens-unavailable",
            _ => null,
        };

        if (error != null)
            return Loc.GetString(error);

        if (!state.Available)
            return Loc.GetString("antag-tokens-unavailable");

        if (!state.Linked)
            return Loc.GetString("antag-tokens-not-linked");

        if (state.ActiveListing is { } active)
        {
            return Loc.GetString(state.ActiveElsewhere ? "antag-tokens-active-elsewhere" : "antag-tokens-active",
                ("name", ListingName(active)));
        }

        return state.InLobby ? string.Empty : Loc.GetString("antag-tokens-lobby-only");
    }

    private Control CreateRow(AntagTokenStateMessage state, AntagTokenListingState listing, bool canBuy)
    {
        var enoughPlayers = state.Online >= listing.MinPlayers;
        var isActive = state.ActiveListing == listing.Id;

        var name = new Label { Text = ListingName(listing.Id), HorizontalExpand = true };
        name.AddStyleClass(NebulaStyleClass.TextValue);

        var text = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Children = { name },
        };

        if (!enoughPlayers)
        {
            var reason = new Label
            {
                Text = Loc.GetString("antag-tokens-not-enough-players",
                    ("online", state.Online),
                    ("required", listing.MinPlayers)),
            };
            reason.AddStyleClass(NebulaStyleClass.TextMuted);
            text.AddChild(reason);
        }

        var action = CreateAction(state, listing, isActive, canBuy && enoughPlayers);

        var row = new ShapedPanel { Orientation = BoxContainer.LayoutOrientation.Vertical };
        row.AddStyleClass(NebulaStyleClass.Section);
        row.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new Thickness(12, 8),
            SeparationOverride = 8,
            Children = { text, action },
        });

        return row;
    }

    private ShapedButton CreateAction(AntagTokenStateMessage state, AntagTokenListingState listing, bool isActive,
        bool canBuy)
    {
        if (isActive)
        {
            var cancel = new ShapedButton
            {
                Text = Loc.GetString("antag-tokens-cancel"),
                Disabled = !state.InLobby || state.ActiveElsewhere,
            };
            cancel.AddStyleClass(NebulaStyleClass.ButtonTonal);
            cancel.OnPressed += _ => CancelPressed?.Invoke();
            return cancel;
        }

        var buy = new ShapedButton
        {
            Text = Loc.GetString("antag-tokens-buy"),
            Disabled = !canBuy,
        };
        buy.AddStyleClass(NebulaStyleClass.ButtonFilled);
        buy.OnPressed += _ => BuyPressed?.Invoke(listing.Id);

        if (!canBuy)
            buy.Content.Modulate = Color.White.WithAlpha(0.5f);

        return buy;
    }

    private string ListingName(ProtoId<AntagTokenListingPrototype> id)
    {
        return _proto.TryIndex(id, out var listing) && _proto.TryIndex(listing.Antag, out var antag)
            ? Loc.GetString(antag.Name)
            : id.Id;
    }
}
