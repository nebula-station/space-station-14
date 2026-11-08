// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Hands.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.ErpPanel.Requirements;

/// <summary>
/// Requires holding an item matching the whitelist.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class HeldItemRequirement : InvertableErpRequirement
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    public override bool IsAvailable(EntityUid uid, IEntityManager entityManager)
    {
        var hands = entityManager.System<SharedHandsSystem>();
        var whitelist = entityManager.System<EntityWhitelistSystem>();

        var holding = false;
        foreach (var held in hands.EnumerateHeld(uid))
        {
            if (!whitelist.IsValid(Whitelist, held))
                continue;

            holding = true;
            break;
        }

        return Inverted ? !holding : holding;
    }
}
