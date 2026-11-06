// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Objectives.Systems;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Trauma.Server._Nebula.ErpChangeling.Objectives;

public sealed partial class ErpChangelingObjectivesSystem : EntitySystem
{
    [Dependency] private NumberObjectiveSystem _number = default!;
    [Dependency] private TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ErpOrgasmConditionComponent, ObjectiveGetProgressEvent>(OnOrgasmProgress);
        SubscribeLocalEvent<ErpArouseCrewConditionComponent, ObjectiveGetProgressEvent>(OnArouseProgress);
        SubscribeLocalEvent<ErpTakeFormConditionComponent, ObjectiveAfterAssignEvent>(OnTakeFormAssigned);
        SubscribeLocalEvent<ErpTakeFormConditionComponent, ObjectiveGetProgressEvent>(OnTakeFormProgress);
    }

    public void CompleteOrgasm(Entity<ErpOrgasmConditionComponent?> objective)
    {
        if (Resolve(objective, ref objective.Comp))
            objective.Comp.Completed = true;
    }

    private void OnOrgasmProgress(Entity<ErpOrgasmConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = ent.Comp.Completed ? 1f : 0f;
    }

    private void OnArouseProgress(Entity<ErpArouseCrewConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var required = _number.GetTarget(ent);
        args.Progress = required <= 0 ? 1f : Math.Min(1f, ent.Comp.Targets.Count / (float) required);
    }

    private void OnTakeFormAssigned(Entity<ErpTakeFormConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        if (_target.GetTarget(ent, out var target) && TryComp<MindComponent>(target, out var targetMind))
            ent.Comp.Name = targetMind.CharacterName;
    }

    private void OnTakeFormProgress(Entity<ErpTakeFormConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = ent.Comp.Name != null
                        && args.Mind.CurrentEntity is { } body
                        && Name(body) == ent.Comp.Name
            ? 1f
            : 0f;
    }
}
