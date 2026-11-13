// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Humanoid.Markings;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.Humanoid;

/// <summary>
/// Resolves <see cref="MarkingRecommendationPrototype"/> into the markings groups each marking is meant for.
/// </summary>
public sealed partial class MarkingRecommendationSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    private static readonly List<ProtoId<MarkingsGroupPrototype>> NoGroups = [];

    private readonly Dictionary<string, List<ProtoId<MarkingsGroupPrototype>>> _groups = [];

    public override void Initialize()
    {
        base.Initialize();

        CacheGroups();
    }

    [SubscribeLocalEvent]
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<MarkingRecommendationPrototype>() || args.WasModified<MarkingPrototype>())
            CacheGroups();
    }

    /// <summary>
    /// The groups the marking is recommended for. Empty for a marking that suits everyone.
    /// </summary>
    public IReadOnlyList<ProtoId<MarkingsGroupPrototype>> GetGroups(string markingId)
    {
        return _groups.GetValueOrDefault(markingId) ?? NoGroups;
    }

    private void CacheGroups()
    {
        _groups.Clear();

        List<MarkingRecommendationPrototype> recommendations = [];
        foreach (var recommendation in _proto.EnumeratePrototypes<MarkingRecommendationPrototype>())
        {
            recommendations.Add(recommendation);
            foreach (var marking in recommendation.Markings)
            {
                AddGroup(marking, recommendation.Group);
            }
        }

        var byId = new HashSet<string>(_groups.Keys);
        foreach (var marking in _proto.EnumeratePrototypes<MarkingPrototype>())
        {
            if (byId.Contains(marking.ID)
                || marking.Sprites.Count == 0
                || marking.Sprites[0] is not SpriteSpecifier.Rsi rsi)
                continue;

            var path = rsi.RsiPath.ToString();
            foreach (var recommendation in recommendations)
            {
                if (MatchesPath(recommendation, path))
                    AddGroup(marking.ID, recommendation.Group);
            }
        }
    }

    private static bool MatchesPath(MarkingRecommendationPrototype recommendation, string path)
    {
        foreach (var fragment in recommendation.Paths)
        {
            if (path.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void AddGroup(string markingId, ProtoId<MarkingsGroupPrototype> group)
    {
        if (!_groups.TryGetValue(markingId, out var groups))
        {
            groups = [];
            _groups[markingId] = groups;
        }

        if (!groups.Contains(group))
            groups.Add(group);
    }
}
