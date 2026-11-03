// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Targeting;
using Content.Shared.EntityConditions;

namespace Content.Trauma.Shared._Nebula.MartialArts;

/// <summary>
/// True if the entity is aiming at the given body part.
/// </summary>
public sealed partial class TargetingCondition : EntityConditionBase<TargetingCondition>
{
    [DataField(required: true)]
    public TargetBodyPart Target;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return string.Empty;
    }
}

public sealed partial class TargetingConditionSystem : EntityConditionSystem<TargetingComponent, TargetingCondition>
{
    protected override void Condition(Entity<TargetingComponent> ent, ref EntityConditionEvent<TargetingCondition> args)
    {
        args.Result = ent.Comp.Target == args.Condition.Target;
    }
}
