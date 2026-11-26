// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Lathe;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Research.Systems;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Research;

public static class SharedResearchSystemExtensions
{
    public static int GetTierCompletionPercentage(this SharedResearchSystem system,
        TechnologyDatabaseComponent component,
        TechDisciplinePrototype techDiscipline,
        IPrototypeManager prototypeManager)
    {
        var allTech = prototypeManager.EnumeratePrototypes<TechnologyPrototype>()
            .Where(p => p.Discipline == techDiscipline.ID && !p.Hidden).ToList();

        // Nebula start
        if (allTech.Count == 0)
            return 0;

        var researchedVisible = component.ResearchedTechnologies.Count(x =>
        {
            var proto = prototypeManager.Index(x);
            return proto.Discipline == techDiscipline.ID && !proto.Hidden;
        });

        var percentage = researchedVisible / (float) allTech.Count * 100f;
        // Nebula end

        return (int) Math.Clamp(percentage, 0, 100);
    }
}
