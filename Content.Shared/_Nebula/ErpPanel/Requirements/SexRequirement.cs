// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.ErpPanel.Requirements;

[Serializable, NetSerializable]
public sealed partial class SexRequirement : InvertableErpRequirement
{
    [DataField(required: true)]
    public HashSet<Sex> Sexes = new();

    public override bool IsAvailable(EntityUid uid, IEntityManager entityManager)
    {
        var hasSex = entityManager.TryGetComponent<HumanoidProfileComponent>(uid, out var profile)
            && Sexes.Contains(profile.Sex);

        return Inverted ? !hasSex : hasSex;
    }
}
