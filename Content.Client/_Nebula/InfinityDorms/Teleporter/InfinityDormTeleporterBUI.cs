// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.InfinityDorms.Teleporter.Players;
using Content.Client._Nebula.InfinityDorms.Teleporter.Rooms;
using Content.Shared._Nebula.InfinityDorm.Messages;
using Content.Shared._Nebula.InfinityDorms;
using Content.Shared._Nebula.InfinityDorms.Components;
using Content.Shared._Nebula.InfinityDorms.States;
using Robust.Client.UserInterface;

namespace Content.Client._Nebula.InfinityDorms.Teleporter;

public sealed partial class InfinityDormTeleporterBUI(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private InfinityDormTeleporterWindow? _window;
    private InfinityDormPlayersWindow? _playersWindow;
    private List<List<InfinityDormVisitorData>> _players = [];


    private InfinityDormRoomsWindow? _roomsWindow;
    private List<InfinityDormRoomData>? _userRooms;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<InfinityDormTeleporterWindow>();

        EntMan.TryGetComponent<InfinityDormTeleporterComponent>(Owner, out var component);

        if (component != null)
        {
            _window.MaxValueForRoomId = component.MaxValueForRoomId;
            _window.MinValueForRoomId = component.MinValueForRoomId;
        }

        _window.ConfirmPressed += () =>
        {
            Teleport();
            _window.Close();
        };

        _window.PlayersListPressed += OpenPlayersWindow;
        _window.RoomsListPressed += OnRoomsListPressed;
    }

    private void Teleport()
    {
        if (_window == null || _window.CurrentRoomId == null)
            return;

        SendMessage(new InfinityDormTeleportMessage(_window.CurrentRoomId, _window.SelectedNumber));
    }

    private void OpenPlayersWindow()
    {
        if (_playersWindow is { Disposed: false })
        {
            _playersWindow.Close();
            return;
        }

        _playersWindow = new InfinityDormPlayersWindow();
        _playersWindow.OnClose += () => _playersWindow = null;
        _playersWindow.Populate(_players);
        _playersWindow.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not InfinityDormTeleportBUIState cast)
            return;

        _players = cast.Players;
        _playersWindow?.Populate(_players);
    }

    private void OnRoomsListPressed()
    {
        if (_roomsWindow is { Disposed: false })
        {
            _roomsWindow.Close();
            return;
        }

        _roomsWindow = new InfinityDormRoomsWindow();
        _roomsWindow.OnClose += () => _playersWindow = null;
        _roomsWindow.Populate(_userRooms);
        _roomsWindow.OpenCentered();
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (message is not InfinityDormTeleportPrivateBUIMessage msg)
            return;

        _userRooms = msg.Rooms;
        _roomsWindow?.Populate(_userRooms);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);


        _roomsWindow?.Close();
        _playersWindow?.Close();
    }
}
