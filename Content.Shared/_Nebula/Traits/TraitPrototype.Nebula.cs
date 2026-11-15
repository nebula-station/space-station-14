// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid;

namespace Content.Shared.Traits;

public sealed partial class TraitPrototype
{
    [DataField]
    public HashSet<Sex> IncludedSexes = new();

    public bool AllowsSex(Sex sex)
    {
        return IncludedSexes.Count == 0 || IncludedSexes.Contains(sex);
    }
}
