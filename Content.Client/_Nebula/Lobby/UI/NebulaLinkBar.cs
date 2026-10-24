// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Discord.Linking;
using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Client.Info;
using Content.Client.UserInterface.Systems.EscapeMenu;
using Content.Client.UserInterface.Systems.Guidebook;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;

namespace Content.Client._Nebula.Lobby.UI;

public sealed partial class NebulaLinkBar : GridContainer
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IEntitySystemManager _systems = default!;
    [Dependency] private IUriOpener _uriOpener = default!;

    private readonly List<(CVarDef<string> CVar, ShapedButton Button)> _links = [];
    private readonly ShapedButton _linkAccount;

    /// <summary>
    /// Kept from <see cref="EnteredTree"/>: on disconnect entity systems shut down before the lobby screen is removed.
    /// </summary>
    private DiscordLinkingSystem? _linking;

    public NebulaLinkBar()
    {
        IoCManager.InjectDependencies(this);

        Columns = 3;
        HSeparationOverride = 6;
        VSeparationOverride = 6;

        AddAction("server-info-rules-button", () => new RulesAndInfoWindow().Open());
        AddAction("server-info-guidebook-button", () => UserInterfaceManager.GetUIController<GuidebookUIController>().ToggleGuidebook());
        AddAction("nebula-lobby-button-changelog", () => UserInterfaceManager.GetUIController<ChangelogUIController>().ToggleWindow());
        _linkAccount = AddAction("nebula-lobby-button-link-account", () => UserInterfaceManager.GetUIController<DiscordLinkingUIController>().ToggleWindow());

        AddLink("server-info-discord-button", CCVars.InfoLinksDiscord);
        AddLink("server-info-website-button", CCVars.InfoLinksWebsite);
        AddLink("server-info-wiki-button", CCVars.InfoLinksWiki);
        AddLink("server-info-forum-button", CCVars.InfoLinksForum);
        AddLink("server-info-telegram-button", CCVars.InfoLinksTelegram);
        AddLink("nebula-lobby-button-bug-report", CCVars.InfoLinksBugReport);
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        foreach (var (cvar, button) in _links)
        {
            button.Visible = _cfg.GetCVar(cvar) != string.Empty;
        }

        if (!_systems.TryGetEntitySystem(out _linking))
            return;

        _linking.VerifiedChanged += UpdateLinkAccount;
        _linking.RequestVerificationStatus();
        UpdateLinkAccount();
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        if (_linking != null)
            _linking.VerifiedChanged -= UpdateLinkAccount;

        _linking = null;
    }

    private void UpdateLinkAccount()
    {
        var linked = _linking?.IsVerified ?? false;

        _linkAccount.Text = Loc.GetString(linked ? "nebula-lobby-button-discord-linked" : "nebula-lobby-button-link-account");
        _linkAccount.Icon = linked ? NebulaIcons.Get("check_circle") : null;
    }

    private void AddLink(string loc, CVarDef<string> cvar)
    {
        var button = AddAction(loc, () => _uriOpener.OpenUri(_cfg.GetCVar(cvar)));
        _links.Add((cvar, button));
    }

    private ShapedButton AddAction(string loc, Action onPressed)
    {
        var button = new ShapedButton
        {
            Text = Loc.GetString(loc),
            HorizontalExpand = true,
        };

        button.StyleClasses.Add(NebulaStyleClass.ButtonTonal);
        button.OnPressed += _ => onPressed();

        AddChild(button);
        return button;
    }
}
