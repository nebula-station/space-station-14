// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface.Controls;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Nebula.Discord.Linking;

/// <summary>
/// Shows the one-time code the player sends to the Discord bot to link their account.
/// </summary>
public sealed partial class DiscordLinkingWindow : NebulaWindow
{
    public readonly RichTextLabel Label;
    public readonly ShapedButton CopyButton;
    public readonly ShapedButton LinkButton;

    public DiscordLinkingWindow()
    {
        Title = Loc.GetString("rmc-ui-link-discord-account");
        MinWidth = 420;

        Label = new RichTextLabel { HorizontalExpand = true };

        var body = new ShapedPanel
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
        };
        body.AddStyleClass(NebulaStyleClass.Section);
        body.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(14, 16),
            SeparationOverride = 10,
            Children = { Label },
        });

        CopyButton = CreateButton("rmc-ui-link-discord-account-copy", null, NebulaStyleClass.ButtonFilled);
        LinkButton = CreateButton("rmc-ui-link-discord-account-open-channel", null, NebulaStyleClass.ButtonTonal);

        Contents.AddChild(body);
        Contents.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            Children = { CopyButton, LinkButton },
        });
    }
}
