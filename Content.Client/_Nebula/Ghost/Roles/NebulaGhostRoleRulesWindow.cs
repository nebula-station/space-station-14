// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Shared.CCVar;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.Ghost.Roles;

/// <summary>
/// Rules of a ghost role, the request button unlocks after <see cref="CCVars.GhostRoleTime"/> seconds.
/// Replaces <see cref="Content.Client.UserInterface.Systems.Ghost.Controls.Roles.GhostRoleRulesWindow"/>.
/// </summary>
public sealed partial class NebulaGhostRoleRulesWindow : NebulaWindow
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private NebulaThemeManager _theme = default!;

    private readonly ShapedButton _requestButton;
    private float _timer;
    private int _shownTenths = -1;

    public NebulaGhostRoleRulesWindow(string rules, Action<BaseButton.ButtonEventArgs> requestAction)
    {
        IoCManager.InjectDependencies(this);

        Title = Loc.GetString("ghost-roles-window-title");
        SetSize = new Vector2(500, 500);
        MinSize = new Vector2(380, 300);

        _timer = _cfg.GetCVar(CCVars.GhostRoleTime);

        var message = FormattedMessage.FromMarkupPermissive(rules);

        if (_timer > 0f)
        {
            message.PushNewline();
            message.AddMessage(FormattedMessage.FromMarkupPermissive(
                Loc.GetString("ghost-roles-window-rules-footer", ("time", _timer))));
        }

        var text = new RichTextLabel { Margin = new Thickness(12, 10) };
        text.SetMessage(message, _theme.Palette.OnSurface);

        var section = new ShapedPanel { Orientation = BoxContainer.LayoutOrientation.Vertical };
        section.AddStyleClass(NebulaStyleClass.Section);
        section.AddChild(text);

        _requestButton = new ShapedButton
        {
            HorizontalAlignment = HAlignment.Center,
            MinWidth = 200,
            MinHeight = 34,
            Icon = NebulaIcons.Get("person_add"),
            Disabled = _timer > 0f,
        };
        _requestButton.AddStyleClass(NebulaStyleClass.ButtonFilled);
        _requestButton.OnPressed += requestAction;

        Contents.AddChild(new ScrollContainer
        {
            VerticalExpand = true,
            HScrollEnabled = false,
            Children = { section },
        });
        Contents.AddChild(_requestButton);

        UpdateRequestText();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_requestButton.Disabled)
            return;

        _timer -= args.DeltaSeconds;

        if (_timer <= 0f)
            _requestButton.Disabled = false;
        else if (GetTenths() == _shownTenths)
            return;

        UpdateRequestText();
    }

    private int GetTenths()
    {
        return (int) MathF.Round(_timer * 10f);
    }

    private void UpdateRequestText()
    {
        _shownTenths = GetTenths();
        _requestButton.Text = _requestButton.Disabled
            ? Loc.GetString("ghost-roles-window-request-role-button-timer", ("time", $"{_timer:0.0}"))
            : Loc.GetString("ghost-roles-window-request-role-button");
    }
}
