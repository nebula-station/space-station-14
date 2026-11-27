// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Emoting;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Throwing;

namespace Content.Goobstation.Shared.Dash;

public sealed partial class DashActionSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAnimatedEmotesSystem _animatedEmotes = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;

    [SubscribeLocalEvent]
    private void OnDashAction(Entity<DashActionComponent> ent, ref DashActionEvent args)
    {
        // Nebula start
        TryDash(ent, args);
    }

    /// <summary>
    /// Dashes the performer of actions without a dash component, like the mantis blade combo.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnDashBroadcast(DashActionEvent args)
    {
        TryDash(args.Performer, args);
    }

    /// <summary>
    /// Dashes the entity towards the action target. Returns whether the throw started.
    /// </summary>
    public bool TryDash(EntityUid ent, DashActionEvent args)
    {
        // Nebula end
        if (args.Handled)
            return false; // Nebula edit

        if (args.NeedsGravity && _gravity.IsWeightless(ent)) // Nebula edit
            return false; // Nebula edit

        args.Handled = true;
        var vec = (_transform.ToMapCoordinates(args.Target).Position -
                   _transform.GetMapCoordinates(ent).Position).Normalized() * args.Distance;
        var speed = args.Speed;

        if (args.AffectedBySpeed && TryComp<MovementSpeedModifierComponent>(ent, out var speedcomp))
        {
            vec *= speedcomp.CurrentSprintSpeed / speedcomp.BaseSprintSpeed;
            speed *= speedcomp.CurrentSprintSpeed / speedcomp.BaseSprintSpeed;
        }

        var thrown = _throwing.TryThrow(ent, vec, speed, animated: false); // Nebula edit

        if (args.StaminaDrain != null)
            _stamina.TakeStaminaDamage(ent, args.StaminaDrain.Value, visual: false, immediate: false);

        if (args.Emote is {} emote)
            _animatedEmotes.PlayEmoteAnimation(ent, emote);

        return thrown; // Nebula
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<DashActionComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.ActionUid = _actions.AddAction(ent, ent.Comp.ActionProto);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<DashActionComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.ActionUid);
    }
}
