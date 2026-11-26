using Content.Server.Power.EntitySystems;
using Content.Server.Research.Components;
using Content.Shared.Research.Components;
using Content.Shared.Research;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private void InitializeSource()
    {
//        SubscribeLocalEvent<ResearchPointSourceComponent, ResearchServerGetPointsPerSecondEvent>(OnGetPointsPerSecond); // Nebula edit
    }

/* // Nebula edit
    private void OnGetPointsPerSecond(Entity<ResearchPointSourceComponent> source, ref ResearchServerGetPointsPerSecondEvent args)
    {
        // Nebula start
        if (TryComp<ResearchServerControlStatusComponent>(args.Server, out var status) && !status.GenerationEnabled)
            return;
        // Nebula end

        if (CanProduce(source))
            args.Points += source.Comp.PointsPerSecond;
    }
*/

    private bool CanProduce(Entity<ResearchPointSourceComponent> source) // Nebula edit
    {
        return source.Comp.Active && this.IsPowered(source, EntityManager);
    }

    // Nebula start
    [SubscribeLocalEvent] // Nebula edit
    private void OnGetPointsPerSecondByType(Entity<ResearchPointSourceComponent> source, ref ResearchServerGetPointsPerSecondByTypeEvent args)
    {
        if (!CanProduce(source))
            return;

        if (source.Comp.RequiredInfrastructure != null &&
            (!TryComp<TechnologyDatabaseComponent>(args.Server, out var db) ||
             !db.UnlockedInfrastructure.Contains(source.Comp.RequiredInfrastructure)))
        {
            return;
        }

        args.Points.Add(new ResearchPointAmount
        {
            Type = source.Comp.PointType,
            Amount = source.Comp.PointsPerSecond,
        });
    }
    // Nebula end
}
