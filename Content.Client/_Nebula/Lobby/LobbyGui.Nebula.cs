// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Lobby.UI;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Client.Info;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby.UI;

public sealed partial class LobbyGui
{
    public ShapedTextureRect Background => NebulaLobby.Background;
    public Label ServerName => NebulaLobby.ServerName;

    /// <summary>
    /// Both Nebula side panels share one fixed width, so the width vanilla applies from
    /// server.lobby_right_panel_width goes to a control that isn't shown.
    /// </summary>
    public Control RightSide { get; } = new();

    public NebulaCharacterPanel CharacterPreview => NebulaLobby.CharacterPreview;
    public Control CharacterSetupState => NebulaLobby.CharacterSetupState;

    /// <summary>
    /// Vanilla vote popups go here and are never shown, <see cref="NebulaVoteList"/> draws the votes instead.
    /// </summary>
    public BoxContainer VoteContainer { get; } = new();

    public ChatBox Chat => NebulaLobby.Chat;

    public ShapedButton ReadyButton => NebulaLobby.ReadyButton;
    public ShapedButton ObserveButton => NebulaLobby.ObserveButton;
    public ShapedButton AHelpButton => NebulaLobby.AHelpButton;
    public ShapedButton OptionsButton => NebulaLobby.OptionsButton;
    public ShapedButton LeaveButton => NebulaLobby.LeaveButton;

    public Label StartTime => NebulaLobby.StartTime;
    public Label StationTime => NebulaLobby.StationTime;

    /// <summary>
    /// Server currency is not shown in the Nebula lobby, Trauma still writes the balance here.
    /// </summary>
    public Label Balance { get; } = new();

    public ServerInfo ServerInfo => NebulaLobby.ServerInfo;
    public RichTextLabel PlaytimeComment => NebulaLobby.PlaytimeComment;
    public RichTextLabel LobbySong => NebulaLobby.LobbySong;
    public RichTextLabel LobbyBackground => NebulaLobby.LobbyBackground;
    public RichTextLabel LobbyMessageLabel => NebulaLobby.LobbyMessageLabel;
}
