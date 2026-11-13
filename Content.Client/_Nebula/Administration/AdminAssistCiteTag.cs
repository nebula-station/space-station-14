// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Input;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.Administration;

public sealed class AdminAssistCiteTag : IMarkupTagHandler
{
    public const string TagName = "aacite";

    private static readonly Color LinkColor = Color.LightBlue;
    private static readonly Color HoverColor = Color.LightSkyBlue;

    public string Name => TagName;

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        if (!node.Value.TryGetString(out var cite))
        {
            control = null;
            return false;
        }

        var label = new Label
        {
            Text = cite,
            FontColorOverride = LinkColor,
            MouseFilter = Control.MouseFilterMode.Stop,
        };

        if (node.Attributes.TryGetValue("line", out var line) && line.TryGetString(out var text))
            label.ToolTip = text;

        if (node.Attributes.TryGetValue("summary", out var summary) && summary.TryGetLong(out var summaryId))
        {
            label.DefaultCursorShape = Control.CursorShape.Hand;
            label.OnMouseEntered += _ => label.FontColorOverride = HoverColor;
            label.OnMouseExited += _ => label.FontColorOverride = LinkColor;
            label.OnKeyBindDown += args =>
            {
                if (args.Function != EngineKeyFunctions.UIClick)
                    return;

                IoCManager.Resolve<IEntityManager>()
                    .System<AdminAssistBriefingSystem>()
                    .OpenCitation(summaryId.Value, cite);
                args.Handle();
            };
        }

        control = label;
        return true;
    }
}
