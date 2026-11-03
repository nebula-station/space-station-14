// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Nebula.Humanoid.Markings;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Client.Humanoid;

public sealed partial class MarkingsViewModel
{
    /// <summary>
    /// The gradient to carry over to the next marking of a layer, the same way the colors are carried over.
    /// </summary>
    private readonly Dictionary<HumanoidVisualLayers, MarkingGradient?> _previousLayerGradients = [];

    public void TrySetMarkingGradient(
        ProtoId<OrganCategoryPrototype> organ,
        HumanoidVisualLayers layer,
        ProtoId<MarkingPrototype> markingId,
        MarkingGradient? gradient)
    {
        if (!TryGetLayerMarking(organ, layer, markingId, out var markings, out var index))
            return;

        markings[index] = markings[index] with { Gradient = gradient };
        _previousLayerGradients[layer] = gradient;
        MarkingsChanged?.Invoke(organ, layer);
    }

    /// <summary>
    /// Exchanges the gradient's color with the marking's first color. The marking's alpha stays where it is.
    /// </summary>
    public void TrySwapMarkingGradient(
        ProtoId<OrganCategoryPrototype> organ,
        HumanoidVisualLayers layer,
        ProtoId<MarkingPrototype> markingId)
    {
        if (!TryGetLayerMarking(organ, layer, markingId, out var markings, out var index)
            || markings[index].Gradient is not { } gradient
            || markings[index].MarkingColors.Count == 0)
            return;

        var first = markings[index].MarkingColors[0];
        var swapped = gradient with { Color = first.WithAlpha(1f) };
        markings[index] = markings[index].WithColorAt(0, gradient.Color.WithAlpha(first.A)) with { Gradient = swapped };
        _previousLayerGradients[layer] = swapped;
        _previousLayerColors[layer] = markings[index].MarkingColors.ToList();
        MarkingsChanged?.Invoke(organ, layer);
    }

    /// <summary>
    /// Recolors every sprite of every marking, keeping each sprite's alpha. Colors remembered from removed markings
    /// are dropped, so a marking picked afterwards doesn't bring back the old color.
    /// </summary>
    public void SetAllMarkingColors(Color color)
    {
        foreach (var layers in _markings.Values)
        {
            foreach (var markings in layers.Values)
            {
                for (var i = 0; i < markings.Count; i++)
                {
                    var marking = markings[i];
                    for (var j = 0; j < marking.MarkingColors.Count; j++)
                    {
                        marking = marking.WithColorAt(j, color.WithAlpha(marking.MarkingColors[j].A));
                    }

                    markings[i] = marking;
                }
            }
        }

        _previousColors.Clear();
        _previousLayerColors.Clear();
        MarkingsReset?.Invoke();
    }

    private bool TryGetLayerMarking(
        ProtoId<OrganCategoryPrototype> organ,
        HumanoidVisualLayers layer,
        ProtoId<MarkingPrototype> markingId,
        out List<Marking> markings,
        out int index)
    {
        markings = [];
        index = -1;

        if (!_markings.TryGetValue(organ, out var markingSet)
            || !markingSet.TryGetValue(layer, out var layerMarkings))
            return false;

        markings = layerMarkings;
        index = layerMarkings.FindIndex(it => it.MarkingId == markingId);
        return index != -1;
    }
}
