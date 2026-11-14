// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.GameTicking.Rules;

public abstract partial class NukeopsRuleSystem
{
    /// <summary>
    /// Marks every active nukeops rule as having left the outpost, which forbids declaring war.
    /// </summary>
    public void MarkLeftOutpost()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var nukeops, out _, out _))
        {
            nukeops.LeftOutpost = true;
        }
    }
}
