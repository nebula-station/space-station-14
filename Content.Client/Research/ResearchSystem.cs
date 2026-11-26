using Content.Shared.Research.Systems;
// Nebula start
using Content.Shared.Research;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
// Nebula end

namespace Content.Client.Research;

// Nebula start
public sealed partial class ResearchSystem : SharedResearchSystem
{
    public List<ResearchPointAmount> GetTechnologyFinalPointCostsForUi(TechnologyDatabaseComponent database, TechnologyPrototype technology)
    {
        return GetTechnologyFinalPointCosts(database, technology);
    }
}
// Nebula end
