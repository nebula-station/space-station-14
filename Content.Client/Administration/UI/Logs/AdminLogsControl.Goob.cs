// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Client/Administration/UI/Logs/AdminLogsControl.Goob.cs

using Content.Client.Administration.UI.CustomControls;
using Content.Goobstation.Shared.Paint;
using Content.Shared.Database;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Administration.UI.Logs;

public sealed partial class AdminLogsControl
{
    private void GoobAddLogControls(AdminLogLabel label)
    {
        if (label.Log.Type != LogType.Paint || !PaintLogToken.TryFind(label.Log.Message, out var token))
            return;

        var round = SelectedRoundId;
        var button = new Button
        {
            Text = Loc.GetString("paint-log-view-button"),
            HorizontalAlignment = HAlignment.Left,
            Visible = label.Visible,
        };

        button.OnPressed += _ => IoCManager.Resolve<IEntityManager>()
            .EntityNetManager
            .SendSystemNetworkMessage(new PaintLogImageRequestEvent(round, token));
        label.OnVisibilityChanged += control => button.Visible = control.Visible;
        LogsContainer.AddChild(button);
    }
}
