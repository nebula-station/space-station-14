// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Sponsor;
using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Client._Nebula.UserInterface;
using Robust.Client.UserInterface;

namespace Content.Client._Nebula.Lobby.UI;

/// <summary>
/// Button of the lobby side panel that opens <see cref="NebulaSupportWindow"/>, shows the level for subscribers.
/// </summary>
public sealed partial class NebulaSupportButton : Control
{
    [Dependency] private IEntitySystemManager _systems = default!;

    private readonly ShapedButton _button;

    /// <summary>
    /// Kept from <see cref="EnteredTree"/>: on disconnect entity systems shut down before the lobby screen is
    /// removed.
    /// </summary>
    private SponsorSystem? _sponsors;

    private NebulaSupportWindow? _window;

    public NebulaSupportButton()
    {
        IoCManager.InjectDependencies(this);

        _button = new ShapedButton { HorizontalExpand = true };
        _button.AddStyleClass(NebulaStyleClass.ButtonSupport);
        _button.OnPressed += _ => OpenWindow();
        AddChild(_button);
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        if (!_systems.TryGetEntitySystem(out _sponsors))
            return;

        _sponsors.StatusChanged += Refresh;
        _sponsors.RequestStatus();
        Refresh();
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        _sponsors?.StatusChanged -= Refresh;

        _sponsors = null;

        _window?.Close();
    }

    private void OpenWindow()
    {
        if (_window != null)
        {
            _window.MoveToFront();
            return;
        }

        _window = new NebulaSupportWindow();
        _window.OnClose += () => _window = null;
        _window.OpenCentered();
    }

    private void Refresh()
    {
        var rank = _sponsors?.Rank;

        _button.Icon = NebulaIcons.Get(rank != null ? "workspace_premium" : "favorite");
        _button.Text = rank is { } level
            ? Loc.GetString("nebula-lobby-boosty-subscriber", ("level", level))
            : Loc.GetString("nebula-lobby-boosty-support");
    }
}
