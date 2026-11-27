// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/b52ef462f193de8563488db184be5393b9881342/Content.Client/Administration/UI/CustomControls/AdminLogLabel.cs

using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Shared.Utility;

namespace Content.Client.Administration.UI.CustomControls;

public sealed partial class AdminLogLabel
{
    /// <summary>
    /// The log line with a marker colored by its impact (Orion #434).
    /// </summary>
    private static FormattedMessage FormatLog(SharedAdminLog log, DateTime localTime)
    {
        var formatted = new FormattedMessage();
        formatted.AddMarkupOrThrow($"[color={GetImpactColor(log.Impact)}]█[/color] [bold]{localTime:HH:mm:ss}[/bold]: ");
        formatted.AddText(log.Message);
        return formatted;
    }

    private static string GetImpactColor(LogImpact impact) => impact switch
    {
        LogImpact.Extreme => "red",
        LogImpact.High => "orange",
        LogImpact.Medium => "yellow",
        LogImpact.Low => "lightgreen",
        _ => "gray"
    };
}
