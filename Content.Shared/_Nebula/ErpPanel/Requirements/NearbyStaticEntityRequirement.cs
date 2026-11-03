// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.ErpPanel.Requirements;

/// <summary>
/// An anchored entity matching the whitelist is nearby, e.g. a wall to pin someone against.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class NearbyStaticEntityRequirement : InvertableErpRequirement
{
    [DataField]
    public float Range = 0.5f;

    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    public override bool IsAvailable(EntityUid uid, IEntityManager entityManager)
    {
        var lookupSystem = entityManager.System<EntityLookupSystem>();
        var containerSystem = entityManager.System<SharedContainerSystem>();
        var whitelistSystem = entityManager.System<EntityWhitelistSystem>();

        var hasNearbyStatic = false;

        foreach (var entity in lookupSystem.GetEntitiesInRange(uid, Range))
        {
            if (entity == uid)
                continue;

            if (containerSystem.IsEntityInContainer(entity))
                continue;

            if (!entityManager.TryGetComponent<PhysicsComponent>(entity, out var physics) ||
                physics.BodyType != BodyType.Static)
                continue;

            if (!whitelistSystem.IsValid(Whitelist, entity))
                continue;

            hasNearbyStatic = true;
            break;
        }

        return Inverted ? !hasNearbyStatic : hasNearbyStatic;
    }
}
