// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.ERP;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.ErpPanel.Requirements;

/// <summary>
/// Requires the entity to have fluid stains on it, see <see cref="CumOverlayComponent"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CumOverlayRequirement : InvertableErpRequirement
{
    public override bool IsAvailable(EntityUid uid, IEntityManager entityManager)
    {
        var hasOverlay = entityManager.HasComponent<CumOverlayComponent>(uid);
        return Inverted ? !hasOverlay : hasOverlay;
    }
}
