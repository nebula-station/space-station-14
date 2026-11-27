// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/02b6d617ada28d2a78cfd227b9860838fb082909/Content.Arcane.Server/Objectives/Systems/ResearchDiskConditionSystem.cs

using Content.Server._Arcane.Objectives.Components;
using Content.Server._Arcane.Research.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._Arcane.Objectives.Systems;

public sealed partial class ResearchDiskConditionSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedObjectivesSystem _objectives = default!;

    private EntityQuery<ContainerManagerComponent> _containerQuery;

    public override void Initialize()
    {
        _containerQuery = GetEntityQuery<ContainerManagerComponent>();
    }

    [SubscribeLocalEvent]
    private void OnAssigned(Entity<ResearchDiskConditionComponent> condition, ref ObjectiveAssignedEvent args)
    {
        condition.Comp.RequiredTechnologyCount = _random.Next(condition.Comp.MinTechnologyCount, condition.Comp.MaxTechnologyCount + 1);
    }

    [SubscribeLocalEvent]
    private void OnAfterAssign(Entity<ResearchDiskConditionComponent> condition, ref ObjectiveAfterAssignEvent args)
    {
        var count = condition.Comp.RequiredTechnologyCount;
        _metaData.SetEntityName(condition.Owner, Loc.GetString(condition.Comp.ObjectiveText, ("count", count)), args.Meta);
        _metaData.SetEntityDescription(condition.Owner, Loc.GetString(condition.Comp.DescriptionText, ("count", count)), args.Meta);
        _objectives.SetIcon(condition.Owner, new SpriteSpecifier.Texture(new("/Textures/_Arcane/Objects/Specific/Research/data_disk.rsi/icon.png")), args.Objective);
    }

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<ResearchDiskConditionComponent> condition, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress((args.MindId, args.Mind), condition.Comp);
    }

    private float GetProgress(Entity<MindComponent> mind, ResearchDiskConditionComponent condition)
    {
        if (!_containerQuery.TryGetComponent(mind.Comp.OwnedEntity, out var currentManager))
            return 0f;

        Stack<ContainerManagerComponent> containerStack = [];
        var bestCount = 0;

        do
        {
            foreach (var container in currentManager.Containers.Values)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    if (TryComp<ResearchDataDiskComponent>(entity, out var disk) && disk.StoredTechnologies.Count > bestCount)
                        bestCount = disk.StoredTechnologies.Count;

                    if (_containerQuery.TryGetComponent(entity, out var nested))
                        containerStack.Push(nested);
                }
            }
        } while (containerStack.TryPop(out currentManager));

        return Math.Clamp(bestCount / (float) condition.RequiredTechnologyCount, 0f, 1f);
    }
}
