// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Input;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.Administration;

public sealed class AdminAssistPlayerTag : IMarkupTagHandler
{
    public const string TagName = "aaplayer";

    private static readonly Color LinkColor = Color.Khaki;
    private static readonly Color HoverColor = Color.Gold;

    public string Name => TagName;

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        if (!node.Value.TryGetString(out var name)
            || !node.Attributes.TryGetValue("user", out var userParam)
            || !userParam.TryGetString(out var userText)
            || !Guid.TryParse(userText, out var user))
            return false;

        var label = new Label
        {
            Text = name,
            FontColorOverride = LinkColor,
            MouseFilter = Control.MouseFilterMode.Stop,
            DefaultCursorShape = Control.CursorShape.Hand,
            ToolTip = Loc.GetString("admin-assist-briefing-player-tooltip"),
        };
        label.OnMouseEntered += _ => label.FontColorOverride = HoverColor;
        label.OnMouseExited += _ => label.FontColorOverride = LinkColor;
        label.OnKeyBindDown += args =>
        {
            if (args.Function != EngineKeyFunctions.UIClick)
                return;

            IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand($"playerpanel \"{user}\"");
            args.Handle();
        };

        control = label;
        return true;
    }
}
