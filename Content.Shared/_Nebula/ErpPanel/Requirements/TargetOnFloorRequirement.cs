// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Standing;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.ErpPanel.Requirements;

[Serializable, NetSerializable]
public sealed partial class TargetOnFloorRequirement : InvertableErpRequirement
{
    public override bool IsAvailable(EntityUid uid, IEntityManager entityManager)
    {
        var isOnFloor = entityManager.System<StandingStateSystem>().IsDown(uid);

        return Inverted ? !isOnFloor : isOnFloor;
    }
}
