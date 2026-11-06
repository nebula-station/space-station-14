// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client.Examine;

public sealed partial class ExamineSystem
{
    /// <summary>
    /// Opens the examine tooltip with a custom control instead of a text message.
    /// </summary>
    public void SendExamineTooltip(EntityUid player, EntityUid target, Control content, bool getVerbs, bool centerAtCursor, float maxWidth)
    {
        OpenTooltip(player, target, centerAtCursor);

        if (_examineTooltipOpen is not { } popup)
            return;

        var vBox = popup.GetChild(0).GetChild(0);
        popup.MaxWidth = maxWidth;
        vBox.MaxWidth = maxWidth;
        vBox.AddChild(content);

        UpdateTooltipInfo(player, target, new FormattedMessage(), getVerbs: getVerbs);
    }
}
