// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared.Research.Prototypes;

public sealed partial class TechnologyPrototype
{
    /// <summary>
    /// Point type paid by technologies that only set <see cref="Cost"/>.
    /// </summary>
    public const string LegacyPointType = "General";

    void ISerializationHooks.AfterDeserialization()
    {
        if (PointCosts.Count == 0 && Cost > 0)
            PointCosts.Add(new ResearchPointAmount { Type = LegacyPointType, Amount = Cost });
    }
}
