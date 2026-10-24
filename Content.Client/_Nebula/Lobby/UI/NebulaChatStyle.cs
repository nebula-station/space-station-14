// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Chat.Controls;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Robust.Client.Graphics;

namespace Content.Client._Nebula.Lobby.UI;

/// <summary>
/// Swaps the style classes of a vanilla <see cref="ChatBox"/> for Nebula ones and puts its input into a pill.
/// </summary>
public static class NebulaChatStyle
{
    public static void Apply(ChatBox chat)
    {
        chat.ChatWindowPanel.PanelOverride = new StyleBoxEmpty();

        var input = chat.ChatInput;
        input.RemoveStyleClass(ChatInputBox.StyleClassChatPanel);
        input.Margin = new Thickness(4, 2);

        input.Input.RemoveStyleClass(ChatInputBox.StyleClassChatLineEdit);
        input.Input.AddStyleClass(NebulaStyleClass.ChatLineEdit);

        input.ChannelSelector.RemoveStyleClass(ChannelSelectorItemButton.StyleClassChatSelectorOptionButton);
        input.ChannelSelector.AddStyleClass(NebulaStyleClass.ChatChip);

        input.FilterButton.RemoveStyleClass(ChatInputBox.StyleClassChatFilterOptionButton);
        input.FilterButton.AddStyleClass(NebulaStyleClass.ChatIconButton);

        if (input.Parent is not { } parent)
            return;

        var index = input.GetPositionInParent();
        var pill = new ShapedPanel { HorizontalExpand = true };
        pill.AddStyleClass(NebulaStyleClass.ChatInput);

        parent.RemoveChild(input);
        pill.AddChild(input);
        parent.AddChild(pill);
        pill.SetPositionInParent(index);
    }
}
