using System.Linq;
using Content.Shared.Interaction;
using Content.Server.Popups;
using Content.Shared.Research.Prototypes;
using Content.Server.Research.Systems;
using Content.Shared.Research.Components;
using Content.Shared.Research;

namespace Content.Server.Research.Disk
{
    public sealed partial class ResearchDiskSystem : EntitySystem
    {
        [Dependency] private PopupSystem _popupSystem = default!;
        [Dependency] private ResearchSystem _research = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ResearchDiskComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<ResearchDiskComponent, MapInitEvent>(OnMapInit);
        }

        private void OnAfterInteract(EntityUid uid, ResearchDiskComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            if (!TryComp<ResearchServerComponent>(args.Target, out var server))
                return;

            // Nebula start
            if (component.PointBalances.Count > 0)
            {
                foreach (var balance in component.PointBalances)
                {
                    _research.ModifyServerPoints(args.Target.Value, balance.Type, balance.Amount, server);
                }
            }
            else
            {
                _research.ModifyServerPoints(args.Target.Value, component.Points, server);
            }
            // Nebula end

            _research.LogNetworkEvent(args.Target.Value, "disk", Loc.GetString("research-netlog-disk-points-applied", ("points", component.Points)), args.User); // Nebula edit
            _popupSystem.PopupEntity(Loc.GetString("research-disk-inserted", ("points", component.Points)), args.Target.Value, args.User);
            QueueDel(uid);
            args.Handled = true;
        }

        private void OnMapInit(EntityUid uid, ResearchDiskComponent component, MapInitEvent args)
        {
            if (!component.UnlockAllTech)
                return;

            component.Points = ProtoMan.EnumeratePrototypes<TechnologyPrototype>()
                // Nebula start
                .Sum(tech => tech.PointCosts
                    .Where(cost => cost.Type == "General")
                    .Sum(cost => cost.Amount));
                // Nebula end

                // Nebula start
                component.PointBalances = ProtoMan.EnumeratePrototypes<TechnologyPrototype>()
                    .SelectMany(tech => tech.PointCosts)
                    .GroupBy(cost => cost.Type)
                    .Select(group => new ResearchPointAmount
                    {
                        Type = group.Key,
                        Amount = group.Sum(cost => cost.Amount),
                    })
                    .ToList();
                // Nebula end
        }
    }
}
