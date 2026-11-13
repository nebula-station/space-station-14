// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Network;

namespace Content.Client._Nebula.Administration;

public sealed class AdminAssistRequestRow : BoxContainer
{
    private readonly Button _request;

    public NetUserId Channel { get; }

    public event Action<NetUserId>? OnRequestBriefing;

    public AdminAssistRequestRow(NetUserId channel)
    {
        Channel = channel;
        Orientation = LayoutOrientation.Horizontal;
        Margin = new Thickness(0, 0, 0, 4);

        _request = new Button
        {
            Text = Loc.GetString("admin-assist-request"),
            ToolTip = Loc.GetString("admin-assist-request-tooltip"),
        };
        _request.OnPressed += _ => OnRequestBriefing?.Invoke(Channel);

        AddChild(new Control { HorizontalExpand = true });
        AddChild(_request);
    }

    public void SetRequested(bool pending)
    {
        _request.Disabled = pending;
        _request.Text = Loc.GetString(pending ? "admin-assist-request-pending" : "admin-assist-request");
    }
}
