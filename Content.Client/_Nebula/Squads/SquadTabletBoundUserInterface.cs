// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Squads;
using Robust.Client.UserInterface;

namespace Content.Client._Nebula.Squads;

public sealed class SquadTabletBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private SquadTabletWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SquadTabletWindow>();
        _window.Monitor.SetGrid(EntMan.GetComponentOrNull<TransformComponent>(Owner)?.GridUid);

        _window.Monitor.PlaceOrderPressed += (squad, order, coordinates)
            => SendMessage(new SquadTabletPlaceOrderMessage(squad, order, coordinates));
        _window.Monitor.CancelOrderPressed += order => SendMessage(new SquadTabletCancelOrderMessage(order));
        _window.Monitor.VoiceCommandPressed += (squad, command)
            => SendMessage(new SquadTabletVoiceCommandMessage(squad, command));
        _window.Roster.CreateSquadPressed += (name, color, members)
            => SendMessage(new SquadTabletCreateSquadMessage(name, color, members));
        _window.Roster.DeleteSquadPressed += squad => SendMessage(new SquadTabletDeleteSquadMessage(squad));
        _window.Roster.AssignPressed += (record, squad) => SendMessage(new SquadTabletAssignMessage(record, squad));
        _window.Roster.SetLeaderPressed += (squad, record) => SendMessage(new SquadTabletSetLeaderMessage(squad, record));
        _window.Tasks.CreateTaskPressed += (squad, record, title, description)
            => SendMessage(new SquadTabletCreateTaskMessage(squad, record, title, description));
        _window.Tasks.DeleteTaskPressed += task => SendMessage(new SquadTabletDeleteTaskMessage(task));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is SquadTabletState tablet)
            _window?.UpdateState(tablet);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is SquadTabletTimerFinishedMessage finished)
            _window?.TimerFinished(finished.Name);
    }
}
