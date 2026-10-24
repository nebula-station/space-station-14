// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Ghost.Roles;
using Content.Client.Eui;
using Content.Shared.Eui;

namespace Content.Trauma.Client.Ghost.UI;

public sealed class GhostroleAlertEui : BaseEui
{
    private readonly NebulaGhostRoleAlertWindow _menu; // Nebula edit

    public GhostroleAlertEui()
    {
        _menu = new NebulaGhostRoleAlertWindow(); // Nebula edit
        _menu.OnClose += () => SendMessage(new CloseEuiMessage()); // Nebula edit
    }

    public override void Opened()
    {
        _menu.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _menu.Close();
    }
}
