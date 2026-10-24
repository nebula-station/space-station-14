// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.ErpPanel.Requirements;

[Serializable, NetSerializable]
public sealed partial class NearbyTargetRequirement : InvertableErpRequirement
{
    [DataField(required: true)]
    public HashSet<ProtoId<TagPrototype>> Tags = new();

    [DataField]
    public float Range = 0.5f;

    public override bool IsAvailable(EntityUid uid, IEntityManager entityManager)
    {
        var lookupSystem = entityManager.System<EntityLookupSystem>();
        var tagSystem = entityManager.System<TagSystem>();
        var containerSystem = entityManager.System<SharedContainerSystem>();

        var hasNearbyTarget = false;

        foreach (var entity in lookupSystem.GetEntitiesInRange(uid, Range))
        {
            if (entity == uid)
                continue;

            if (containerSystem.IsEntityInContainer(entity))
                continue;

            if (!tagSystem.HasAnyTag(entity, Tags))
                continue;

            hasNearbyTarget = true;
            break;
        }

        return Inverted ? !hasNearbyTarget : hasNearbyTarget;
    }
}
