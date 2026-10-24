// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Client.Stylesheets;

public sealed partial class StylesheetManager
{
    /// <summary>
    /// Rebuilds every stylesheet from its sheetlets and swaps the interface over to the new ones.
    /// </summary>
    /// <remarks>
    /// Controls that hold a stylesheet themselves keep the old instance until they are recreated.
    /// </remarks>
    public void Reload()
    {
        _styleRuleCount = 0;
        Initialize();
    }
}
