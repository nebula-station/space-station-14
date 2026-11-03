// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.ErpPanel;
using Robust.Client.UserInterface;

namespace Content.Client._Nebula.ErpPanel;

public sealed class ErpPanelWindowBUI(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private ErpPanelWindow? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ErpPanelWindow>();
        _menu.OnSendEmote += (interaction, privateMode) => SendMessage(new ErpPanelSendMessage(interaction, privateMode));
        _menu.OnSendCustomEmote += (emote, privateMode) => SendMessage(new ErpPanelCustomEmoteMessage(emote, privateMode));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_menu is null)
            return;

        if (state is ErpPanelBuiState newState)
            _menu.SetTarget(newState.User, newState.Target);
    }
}
