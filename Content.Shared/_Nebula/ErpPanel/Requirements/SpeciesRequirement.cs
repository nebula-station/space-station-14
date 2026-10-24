// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.ErpPanel.Requirements;

[Serializable, NetSerializable]
public sealed partial class SpeciesRequirement : InvertableErpRequirement
{
    [DataField(required: true)]
    public HashSet<ProtoId<SpeciesPrototype>> Species = new();

    public override bool IsAvailable(EntityUid uid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<HumanoidProfileComponent>(uid, out var profile))
            return false;

        var hasSpecies = Species.Contains(profile.Species);

        return Inverted ? !hasSpecies : hasSpecies;
    }
}
