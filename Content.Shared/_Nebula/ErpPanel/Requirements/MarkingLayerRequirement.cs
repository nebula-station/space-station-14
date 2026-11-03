// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Humanoid;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.ErpPanel.Requirements;

/// <summary>
/// The entity has a marking on the given layer, like a tail or wings.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class MarkingLayerRequirement : InvertableErpRequirement
{
    [DataField(required: true)]
    public HumanoidVisualLayers Layer;

    public override bool IsAvailable(EntityUid uid, IEntityManager entityManager)
    {
        var hasMarking = HasMarking(uid, entityManager);

        return Inverted ? !hasMarking : hasMarking;
    }

    // markings live on the organs, the same container SharedVisualBodySystem reads
    private bool HasMarking(EntityUid uid, IEntityManager entityManager)
    {
        if (!entityManager.System<SharedContainerSystem>().TryGetContainer(uid, BodyComponent.ContainerID, out var organs))
            return false;

        foreach (var organ in organs.ContainedEntities)
        {
            if (entityManager.TryGetComponent<VisualOrganMarkingsComponent>(organ, out var markings) &&
                markings.Markings.TryGetValue(Layer, out var layerMarkings) &&
                layerMarkings.Count > 0)
                return true;
        }

        return false;
    }
}
