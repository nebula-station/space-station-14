// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Shared.Administration.Managers;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Nebula.Lobby.UI;

/// <summary>
/// Confirmation before joining the round as an observer.
/// Replaces <see cref="Content.Client.Lobby.UI.ObserveWarningWindow"/>.
/// </summary>
public sealed partial class NebulaObserveWindow : NebulaWindow
{
    [Dependency] private ISharedAdminManager _adminManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IClientConsoleHost _console = default!;

    public NebulaObserveWindow()
    {
        IoCManager.InjectDependencies(this);

        Title = Loc.GetString("nebula-observe-title");
        MinWidth = 380;

        var heading = new Label
        {
            Text = Loc.GetString("observe-warning-1"),
            Align = Label.AlignMode.Center,
        };
        heading.AddStyleClass(NebulaStyleClass.TextValue);

        var warning = new Label
        {
            Text = Loc.GetString("observe-warning-2"),
            Align = Label.AlignMode.Center,
        };
        warning.AddStyleClass(NebulaStyleClass.TextMuted);

        var body = new ShapedPanel
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
        };
        body.AddStyleClass(NebulaStyleClass.Section);

        var bodyContent = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(14, 16),
            SeparationOverride = 6,
            Children = { heading, warning },
        };
        body.AddChild(bodyContent);

        var nevermind = CreateButton("observe-nevermind", null, NebulaStyleClass.ButtonGhost);
        nevermind.OnPressed += _ => Close();

        var observe = CreateButton("observe-confirm", "visibility", NebulaStyleClass.ButtonFilled);
        observe.OnPressed += _ => Run("observe");

        var buttons = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            Children = { nevermind, observe },
        };

        if (_playerManager.LocalSession is { } player && _adminManager.IsAdmin(player))
        {
            observe.Text = Loc.GetString("observe-as-player");

            var admin = CreateButton("observe-as-admin", "admin_panel_settings", NebulaStyleClass.ButtonTonal);
            admin.OnPressed += _ => Run("observe admin");
            buttons.AddChild(admin);
        }

        Contents.AddChild(body);
        Contents.AddChild(buttons);
    }

    private void Run(string command)
    {
        _console.ExecuteCommand(command);
        Close();
    }
}
