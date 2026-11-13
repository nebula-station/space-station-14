// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidCharacterAppearance
{
    private static List<Marking> PickNebulaLayerMarkings(
        HumanoidVisualLayers layer,
        ProtoId<MarkingsGroupPrototype> group,
        MarkingsLimits layerLimits,
        IReadOnlyDictionary<string, MarkingPrototype> allMarkings,
        CharacterPalette palette,
        IRobustRandom random)
    {
        var markingManager = IoCManager.Resolve<MarkingManager>();
        var hair = layer is HumanoidVisualLayers.Hair or HumanoidVisualLayers.FacialHair;
        List<Marking> markings = [];

        foreach (var id in layerLimits.Default)
        {
            if (markings.Count >= layerLimits.Limit)
                break;

            if (!allMarkings.TryGetValue(id, out var proto))
                continue;

            var colors = hair
                ? Enumerable.Repeat(palette.HairColor, proto.Sprites.Count).ToList()
                : MarkingColoring.GetMarkingLayerColors(proto, palette.SkinColor, palette.EyeColor, markings);
            markings.Add(new Marking(proto.ID, colors));
        }

        if (markings.Count >= layerLimits.Limit)
            return markings;

        Dictionary<string, MarkingPrototype> pool = [];
        foreach (var (id, proto) in allMarkings)
        {
            if (markings.Exists(marking => marking.MarkingId.Id == id))
                continue;

            // unrecommended markings outside hair include animal ears and tails
            var groups = markingManager.GetRecommendedGroups(id);
            if (groups.Contains(group) || hair && groups.Count == 0)
                pool[id] = proto;
        }

        if (pool.Count == 0)
            return markings;

        var single = new MarkingsLimits
        {
            Limit = 1,
            Required = layerLimits.Required,
            Weight = layerLimits.Weight,
        };

        markings.AddRange(PickLayerRandomMarkings(layer, single, pool, palette, random));
        return markings;
    }
}
