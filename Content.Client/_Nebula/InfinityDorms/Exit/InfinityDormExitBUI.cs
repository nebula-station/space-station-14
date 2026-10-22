// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.InfinityDorm.Messages;
using Content.Shared._Nebula.InfinityDorms.States;
using Robust.Client.UserInterface;

namespace Content.Client._Nebula.InfinityDorms.Exit;

public sealed class InfinityDormExitBUI(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private InfinityDormExitWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<InfinityDormExitWindow>();

        _window.ClosedToggled += closed => SendMessage(new InfinityDormToggleClosedMessage(closed));
        _window.ExitPressed += () =>
        {
            SendMessage(new InfinityDormExitMessage());
            _window.Close();
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not InfinityDormExitBUIState exitState)
            return;

        _window?.UpdateState(exitState.IsClosed, exitState.IsOwner, exitState.PlayersCount);
    }
}
