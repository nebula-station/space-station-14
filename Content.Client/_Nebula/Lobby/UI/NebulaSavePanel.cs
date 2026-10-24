// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Nebula.Lobby.UI;

/// <summary>
/// Asks whether to save the edited character before leaving the setup screen.
/// Replaces <see cref="Content.Client.Lobby.UI.CharacterSetupGuiSavePanel"/> with the same buttons.
/// </summary>
public sealed class NebulaSavePanel : NebulaWindow
{
    public ShapedButton SaveButton { get; }

    public ShapedButton NoSaveButton { get; }

    public ShapedButton CancelButton { get; }

    public NebulaSavePanel()
    {
        Title = Loc.GetString("character-setup-gui-save-panel-title");
        SetWidth = 420;

        var text = new Label
        {
            Text = Loc.GetString("nebula-save-panel-text"),
            Margin = new Thickness(6, 0, 6, 4),
        };
        text.AddStyleClass(NebulaStyleClass.TextMuted);

        CancelButton = CreateButton("character-setup-gui-save-panel-cancel", null, NebulaStyleClass.ButtonGhost);
        NoSaveButton = CreateButton("character-setup-gui-save-panel-nosave", null, NebulaStyleClass.ButtonTonal);
        NoSaveButton.AddStyleClass(StyleClass.Negative);
        SaveButton = CreateButton("character-setup-gui-save-panel-save", "save", NebulaStyleClass.ButtonFilled);

        CancelButton.OnPressed += _ => Close();

        Contents.AddChild(text);
        Contents.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            Children = { CancelButton, NoSaveButton, SaveButton },
        });
    }
}
