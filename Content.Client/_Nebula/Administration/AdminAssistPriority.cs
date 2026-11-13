// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Nebula.Administration;

public static class AdminAssistPriority
{
    public const int NoTicket = 4;

    private const float StripeWidth = 5;

    public static readonly Color High = Color.FromHex("#FF4040");
    public static readonly Color Normal = Color.FromHex("#FFB020");
    public static readonly Color Low = Color.FromHex("#7CE07C");
    public static readonly Color None = Color.FromHex("#8A8A9A");

    public static int Rank(string? priority)
    {
        return priority switch
        {
            "ВЫСОКИЙ" => 0,
            "ОБЫЧНЫЙ" => 1,
            "НИЗКИЙ" => 2,
            _ => 3,
        };
    }

    public static Color StripeColor(string? priority)
    {
        return Rank(priority) switch
        {
            0 => High,
            1 => Normal,
            2 => Low,
            _ => None,
        };
    }

    public static string Text(string? priority)
    {
        return Loc.GetString(Rank(priority) switch
        {
            0 => "admin-assist-priority-high",
            1 => "admin-assist-priority-normal",
            2 => "admin-assist-priority-low",
            _ => "admin-assist-priority-none",
        });
    }

    public static Control Stripe(string? priority, string? claimedBy)
    {
        var tooltip = Loc.GetString("admin-assist-priority-tooltip", ("priority", Text(priority)));
        if (claimedBy != null)
            tooltip += "\n" + Loc.GetString("admin-assist-claim-by", ("admin", claimedBy));

        return new PanelContainer
        {
            MinWidth = StripeWidth,
            Margin = new Thickness(0, 1, 4, 1),
            VerticalExpand = true,
            MouseFilter = Control.MouseFilterMode.Pass,
            ToolTip = tooltip,
            PanelOverride = new StyleBoxFlat { BackgroundColor = StripeColor(priority) },
        };
    }
}
