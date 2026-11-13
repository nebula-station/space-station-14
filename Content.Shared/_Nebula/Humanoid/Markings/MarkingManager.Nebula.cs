// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Markings;

public sealed partial class MarkingManager
{
    private static readonly List<ProtoId<MarkingsGroupPrototype>> NoRecommendedGroups = [];

    private readonly Dictionary<string, List<ProtoId<MarkingsGroupPrototype>>> _recommendedGroups = [];

    /// <summary>
    /// The groups the marking is recommended for by <see cref="MarkingRecommendationPrototype"/>.
    /// Empty for a marking that suits everyone.
    /// </summary>
    public IReadOnlyList<ProtoId<MarkingsGroupPrototype>> GetRecommendedGroups(string markingId)
    {
        return _recommendedGroups.GetValueOrDefault(markingId) ?? NoRecommendedGroups;
    }

    private void CacheRecommendations()
    {
        _recommendedGroups.Clear();

        List<MarkingRecommendationPrototype> recommendations = [];
        foreach (var recommendation in _prototype.EnumeratePrototypes<MarkingRecommendationPrototype>())
        {
            recommendations.Add(recommendation);
            foreach (var marking in recommendation.Markings)
            {
                AddRecommendedGroup(marking, recommendation.Group);
            }
        }

        var byId = new HashSet<string>(_recommendedGroups.Keys);
        foreach (var marking in _markings.Values)
        {
            if (byId.Contains(marking.ID)
                || marking.Sprites.Count == 0
                || marking.Sprites[0] is not SpriteSpecifier.Rsi rsi)
                continue;

            var path = rsi.RsiPath.ToString();
            foreach (var recommendation in recommendations)
            {
                if (MatchesRecommendation(recommendation, path))
                    AddRecommendedGroup(marking.ID, recommendation.Group);
            }
        }
    }

    private static bool MatchesRecommendation(MarkingRecommendationPrototype recommendation, string path)
    {
        foreach (var fragment in recommendation.Paths)
        {
            if (path.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void AddRecommendedGroup(string markingId, ProtoId<MarkingsGroupPrototype> group)
    {
        if (!_recommendedGroups.TryGetValue(markingId, out var groups))
        {
            groups = [];
            _recommendedGroups[markingId] = groups;
        }

        if (!groups.Contains(group))
            groups.Add(group);
    }
}
