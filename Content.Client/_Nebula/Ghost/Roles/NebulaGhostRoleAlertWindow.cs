// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Client.Ghost;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Nebula.Ghost.Roles;

/// <summary>
/// Tells ghosts that a major antagonist raffle has started and can open the ghost roles window.
/// Replaces <c>Content.Trauma.Client.Ghost.UI.GhostroleAlertMenu</c>.
/// </summary>
public sealed partial class NebulaGhostRoleAlertWindow : NebulaWindow
{
    [Dependency] private IEntitySystemManager _entitySystems = default!;

    public NebulaGhostRoleAlertWindow()
    {
        IoCManager.InjectDependencies(this);

        Title = Loc.GetString("ghost-role-spawned");
        MinWidth = 380;

        var icon = new TextureRect
        {
            Texture = NebulaIcons.Get("Ghost/skull"),
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            SetSize = new Vector2(36, 36),
            VerticalAlignment = VAlignment.Center,
        };
        icon.AddStyleClass(NebulaStyleClass.Icon);
        icon.AddStyleClass(NebulaStyleClass.AccentError);

        var badge = new ShapedPanel
        {
            HorizontalAlignment = HAlignment.Center,
            Align = BoxContainer.AlignMode.Center,
            SetSize = new Vector2(64, 64),
            Children = { icon },
        };
        badge.AddStyleClass(NebulaStyleClass.Card);
        badge.AddStyleClass(NebulaStyleClass.AccentError);

        var heading = new Label
        {
            Text = Loc.GetString("ghost-role-spawned"),
            Align = Label.AlignMode.Center,
        };
        heading.AddStyleClass(NebulaStyleClass.TextValue);

        var text = new Label
        {
            Text = Loc.GetString("ghost-role-spawned-text"),
            Align = Label.AlignMode.Center,
        };
        text.AddStyleClass(NebulaStyleClass.TextMuted);

        var body = new ShapedPanel { Orientation = BoxContainer.LayoutOrientation.Vertical };
        body.AddStyleClass(NebulaStyleClass.Section);
        body.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(14, 16),
            SeparationOverride = 6,
            Children = { badge, heading, text },
        });

        var confirm = CreateButton("ghost-role-confirm", null, NebulaStyleClass.ButtonGhost);
        confirm.OnPressed += _ => Close();

        var openRoles = CreateButton("nebula-ghost-role-alert-open-roles", "person_add", NebulaStyleClass.ButtonFilled);
        openRoles.AddStyleClass(NebulaStyleClass.AccentError);
        openRoles.OnPressed += _ =>
        {
            if (_entitySystems.TryGetEntitySystem<GhostSystem>(out var ghost))
                ghost.OpenGhostRoles();

            Close();
        };

        Contents.AddChild(body);
        Contents.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            Children = { confirm, openRoles },
        });
    }
}
