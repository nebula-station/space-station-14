// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.Movement.Pulling.Components;
using Content.Trauma.Common.MartialArts;

namespace Content.Trauma.Shared._Nebula.MartialArts;

/// <summary>
/// True if the entity is grabbed by the condition's source at the given stage or higher.
/// </summary>
public sealed partial class GrabStageCondition : EntityConditionBase<GrabStageCondition>
{
    [DataField(required: true)]
    public GrabStage Stage;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return string.Empty;
    }
}

public sealed partial class GrabStageConditionSystem : EntityConditionSystem<PullableComponent, GrabStageCondition>
{
    protected override void Condition(Entity<PullableComponent> ent, ref EntityConditionEvent<GrabStageCondition> args)
    {
        args.Result = args.SourceEnt is { } puller
            && ent.Comp.Puller == puller
            && ent.Comp.GrabStage >= args.Condition.Stage;
    }
}
