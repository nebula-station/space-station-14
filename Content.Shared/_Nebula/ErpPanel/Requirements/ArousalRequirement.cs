// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.ERP;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.ErpPanel.Requirements;

[Serializable, NetSerializable]
public sealed partial class ArousalRequirement : InvertableErpRequirement
{
    [DataField]
    public ArousalPhase Minimum = ArousalPhase.Aroused;

    public override bool IsAvailable(EntityUid uid, IEntityManager entityManager)
    {
        // live value instead of CurrentPhase, which lags up to 5 s on decay
        var hasArousal = entityManager.TryGetComponent<ArousalComponent>(uid, out var arousal)
            && arousal.ComputePhase(entityManager.System<ArousalSystem>().GetArousal(arousal)) >= Minimum;

        return Inverted ? !hasArousal : hasArousal;
    }
}
