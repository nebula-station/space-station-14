// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Components;
using Content.Shared.Forensics.Components;
using Content.Shared._Funky.Stains;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Content.Shared.Forensics.Systems;

namespace Content.Server._Funky.Stains;

public sealed partial class StainableSystem : SharedStainableSystem
{
    [Dependency] private ForensicsSystem _forensics = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    protected override void DirtyOwnerAppearance(EntityUid item)
    {
        if (_container.TryGetContainingContainer(item, out var container) && TryComp<AppearanceComponent>(container.Owner, out var appearance))
        {
            Dirty(container.Owner, appearance);
        }
    }

    protected override void StainForensics(Entity<StainableComponent> ent, Entity<SolutionComponent> solution)
    {
        _tag.AddTag(ent.Owner, SharedForensicScannerSystem.DNASolutionScannableTag);
    }

    protected override void WashingForensics(Entity<StainableComponent> ent, Entity<SolutionComponent> solution, EntityUid washingMachine)
    {
        if (!TryComp<ForensicsComponent>(washingMachine, out var forensics))
            return;

        forensics.DNAs.UnionWith(_forensics.GetSolutionsDNA(solution.Comp.Solution));
    }
}
