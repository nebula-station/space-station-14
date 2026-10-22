// SPDX-License-Identifier: AGPL-3.0-or-later

// Ported from Radiant Sector: https://github.com/TetrisZz/frontier-Radiant

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Buckle.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Misc;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Nebula.Leash;

/// <summary>
/// Lets a humanoid holding a leash drag other humanoids around.
/// Uses the same mouse joint + <c>TetherEntity</c> approach as tether guns, but the anchor follows the puller.
/// </summary>
public sealed partial class LeashSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!; // Nebula edit
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedJointSystem _joints = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly EntProtoId TetherPrototype = "TetherEntity";
    private static readonly TimeSpan YankKnockdownTime = TimeSpan.FromSeconds(2);
    private static readonly SpriteSpecifier DetachIcon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/unbuckle.svg.192dpi.png"));
    private static readonly SpriteSpecifier AttachIcon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/group.svg.192dpi.png"));
    private const string JointId = "nebula-leash";
    private const float YankRange = 3f;
    private const float AttachRange = 2.5f;

    // Nebula start
    private const float SnapSlack = 2f;
    // Squared distance an anchor has to drift before it is moved again.
    private const float AnchorEpsilonSquared = 0.0025f;
    // Nebula end

    private readonly List<Entity<LeashPullerComponent, TransformComponent>> _pullers = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidProfileComponent, GetVerbsEvent<InteractionVerb>>(OnTargetVerbs);
        SubscribeLocalEvent<LeashComponent, LeashAttachDoAfterEvent>(OnAttachDoAfter);

        SubscribeLocalEvent<LeashPullerComponent, GetVerbsEvent<InteractionVerb>>(OnPullerVerbs);
        SubscribeLocalEvent<LeashPullerComponent, EntityTerminatingEvent>(OnPullerTerminating);

        // Nebula start
        SubscribeLocalEvent<LeashedComponent, EntityTerminatingEvent>(OnLeashedTerminating);
        SubscribeLocalEvent<LeashedComponent, EntGotInsertedIntoContainerMessage>(OnLeashedInserted);
        SubscribeLocalEvent<LeashedComponent, BuckleAttemptEvent>(OnLeashedBuckleAttempt);
        // Nebula end
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Nebula start
        _pullers.Clear();
        var query = EntityQueryEnumerator<LeashPullerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var puller, out var xform))
        {
            _pullers.Add((uid, puller, xform));
        }

        foreach (var ent in _pullers)
        {
            // Validation and snapping are server-only, the client just predicts anchor positions.
            if (_net.IsServer && !ValidateLeash(ent))
                continue;

            UpdateAnchors(ent);
        }
        // Nebula end
    }

    /// <summary>
    /// Snaps leashes that are no longer valid. Returns false if the puller has no targets left.
    /// </summary>
    private bool ValidateLeash(Entity<LeashPullerComponent, TransformComponent> ent)
    {
        var (uid, puller, xform) = ent;

        if (puller.Targets.Count == 0)
        {
            RemComp<LeashPullerComponent>(uid);
            return false;
        }

        // Nebula edit
        if (!TryComp<LeashComponent>(puller.Leash, out var leash) || !_hands.IsHolding(uid, puller.Leash) ||
            _mob.IsIncapacitated(uid) || _container.IsEntityInContainer(uid))
        {
            PopupSnapped(uid, puller);
            StopAll((uid, puller));
            return false;
        }

        foreach (var target in puller.Targets.ToArray())
        {
            if (!ShouldSnap(xform, target, leash))
                continue;

            PopupSnapped(uid, target);
            StopLeash((uid, puller), target);
        }

        return puller.Targets.Count > 0;
    }

    private bool ShouldSnap(TransformComponent pullerXform, EntityUid target, LeashComponent leash)
    {
        if (!TryComp<LeashedComponent>(target, out var leashed) || Deleted(leashed.Anchor))
            return true;

        var targetXform = Transform(target);
        if (pullerXform.MapID != targetXform.MapID || pullerXform.MapID == MapId.Nullspace)
            return true;

        var distance = (_transform.GetWorldPosition(pullerXform) - _transform.GetWorldPosition(targetXform)).Length();
        return distance > leash.MaxDistance + SnapSlack; // Nebula edit
    }

    /// <summary>
    /// Pulls each target back to leash length once the leash goes taut.
    /// </summary>
    private void UpdateAnchors(Entity<LeashPullerComponent, TransformComponent> puller)
    {
        if (!TryComp<LeashComponent>(puller.Comp1.Leash, out var leash))
            return;

        var pullerPos = _transform.GetWorldPosition(puller.Comp2);

        foreach (var target in puller.Comp1.Targets)
        {
            if (!TryComp<LeashedComponent>(target, out var leashed) || Deleted(leashed.Anchor))
                continue;

            var diff = _transform.GetWorldPosition(target) - pullerPos;
            var distance = diff.Length();

            // Nebula start
            var taut = distance > leashed.Distance && distance > 0.001f;
            if (TryComp<JointComponent>(leashed.Anchor, out var joints) &&
                joints.GetJoints.TryGetValue(JointId, out var joint) &&
                joint is MouseJoint mouse)
            {
                mouse.MaxForce = taut ? leash.MaxForce : 0f;
            }

            if (!taut)
                continue;

            var anchorPos = pullerPos + diff / distance * leashed.Distance;
            if ((_transform.GetWorldPosition(leashed.Anchor) - anchorPos).LengthSquared() > AnchorEpsilonSquared)
                _transform.SetWorldPosition(leashed.Anchor, anchorPos);
            // Nebula end
        }
    }

    #region Verbs

    private void OnTargetVerbs(Entity<HumanoidProfileComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Target == args.User || !args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (!HasComp<HumanoidProfileComponent>(args.User))
            return;

        var user = args.User;
        var target = args.Target;

        if (TryComp<LeashPullerComponent>(user, out var puller) && puller.Targets.Contains(target))
        {
            args.Verbs.Add(new InteractionVerb
            {
                Text = Loc.GetString("leash-verb-detach-target"),
                Icon = DetachIcon,
                Priority = 20,
                Impact = LogImpact.Low,
                Act = () => StopLeash((user, puller), target),
            });
            return;
        }

        if (!TryGetHeldLeash(user, out var leash))
            return;

        var verb = new InteractionVerb
        {
            Text = Loc.GetString("leash-verb-attach"),
            Icon = AttachIcon,
            Priority = 10,
            Impact = LogImpact.Low,
            Act = () => TryStartAttach(user, target),
        };

        if (TryGetAttachBlocker(user, target, leash.Value, out var reason))
        {
            verb.Disabled = true;
            verb.Message = Loc.GetString(reason);
        }

        args.Verbs.Add(verb);
    }

    private void OnPullerVerbs(Entity<LeashPullerComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Target != args.User || ent.Comp.Targets.Count == 0)
            return;

        if (!args.CanInteract || !args.CanComplexInteract)
            return;

        var step = CompOrNull<LeashComponent>(ent.Comp.Leash)?.DistanceStep ?? 0.5f;

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("leash-verb-detach"),
            Icon = DetachIcon,
            Priority = 20,
            Impact = LogImpact.Low,
            Act = () => StopAll(ent),
        });

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("leash-verb-tighten"),
            Priority = 19,
            Impact = LogImpact.Low,
            Act = () => AdjustDistance(ent, -step),
        });

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("leash-verb-loosen"),
            Priority = 18,
            Impact = LogImpact.Low,
            Act = () => AdjustDistance(ent, step),
        });

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("leash-verb-yank"),
            Priority = 17,
            Impact = LogImpact.Low,
            Act = () => YankAll(ent),
        });
    }

    #endregion

    #region Attaching

    private void TryStartAttach(EntityUid user, EntityUid target)
    {
        if (!TryGetHeldLeash(user, out var leash))
        {
            _popup.PopupEntity(Loc.GetString("leash-fail-no-leash"), user, user);
            return;
        }

        if (TryGetAttachBlocker(user, target, leash.Value, out var reason))
        {
            _popup.PopupEntity(Loc.GetString(reason), user, user);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, user, leash.Value.Comp.AttachDelay, new LeashAttachDoAfterEvent(), leash.Value, target, leash.Value)
        {
            NeedHand = true,
            BreakOnDropItem = true,
            BreakOnDamage = true,
            DistanceThreshold = AttachRange,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            _popup.PopupEntity(Loc.GetString("leash-start-attaching"), user, user);
    }

    private void OnAttachDoAfter(Entity<LeashComponent> ent, ref LeashAttachDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        args.Handled = true;

        if (!_net.IsServer)
            return;

        var user = args.User;
        if (!_hands.IsHolding(user, ent))
        {
            _popup.PopupEntity(Loc.GetString("leash-fail-no-leash"), user, user);
            return;
        }

        if (TryGetAttachBlocker(user, target, ent, out var reason))
        {
            _popup.PopupEntity(Loc.GetString(reason), user, user);
            return;
        }

        StartLeash(user, target, ent);
    }

    /// <summary>
    /// Returns true with a locale id for the reason if <paramref name="user"/> can't leash <paramref name="target"/>.
    /// </summary>
    private bool TryGetAttachBlocker(EntityUid user, EntityUid target, Entity<LeashComponent> leash, [NotNullWhen(true)] out string? reason)
    {
        reason = null;

        if (!_mob.IsAlive(user) || !_mob.IsAlive(target))
            reason = "leash-fail-not-alive";
        else if (HasComp<LeashedComponent>(target) || HasComp<TetheredComponent>(target))
            reason = "leash-fail-target-tethered";
        else if (HasComp<LeashedComponent>(user) || HasComp<TetheredComponent>(user))
            reason = "leash-fail-puller-tethered";
        // Nebula edit
        else if (TryComp<LeashPullerComponent>(user, out var puller) &&
                 (puller.Leash != leash.Owner && puller.Targets.Count > 0 || puller.Targets.Count >= leash.Comp.MaxTargets))
            reason = "leash-fail-too-many";
        else if (!TryComp<PhysicsComponent>(target, out var physics))
            reason = "leash-fail-physics";
        else if (physics.BodyType == BodyType.Static || _container.IsEntityInContainer(target))
            reason = "leash-fail-container";
        else if (physics.Mass > leash.Comp.MassLimit)
            reason = "leash-fail-mass";
        else if (TryComp<StrapComponent>(target, out var strap) && strap.BuckledEntities.Count > 0)
            reason = "leash-fail-buckled";
        else if (!_interaction.InRangeUnobstructed(user, target))
            reason = "leash-fail-range";

        return reason != null;
    }

    private void StartLeash(EntityUid user, EntityUid target, Entity<LeashComponent> leash)
    {
        if (!TryComp<PhysicsComponent>(target, out var targetPhysics))
            return;

        var puller = EnsureComp<LeashPullerComponent>(user);
        puller.Leash = leash;
        puller.Targets.Add(target);

        var targetXform = Transform(target);
        _transform.Unanchor(target, targetXform);

        var startDistance = (_transform.GetWorldPosition(user) - _transform.GetWorldPosition(targetXform)).Length();
        var anchor = Spawn(TetherPrototype, _transform.GetMapCoordinates(target, targetXform));

        // Nebula start
        var leashed = EnsureComp<LeashedComponent>(target);
        leashed.Puller = user;
        leashed.Anchor = anchor;
        leashed.Distance = Math.Clamp(startDistance, leash.Comp.MinDistance, leash.Comp.MaxDistance);
        // Nebula end

        // Nebula edit
        _physics.SetSleepingAllowed(target, targetPhysics, false);
        _physics.WakeBody(target, body: targetPhysics);

        var anchorPhysics = Comp<PhysicsComponent>(anchor);
        _physics.WakeBody(anchor, body: anchorPhysics);

        var joint = _joints.CreateMouseJoint(anchor, target, id: JointId);
        SharedJointSystem.LinearStiffness(leash.Comp.Frequency, leash.Comp.DampingRatio, anchorPhysics.Mass, targetPhysics.Mass,
            out var stiffness, out var damping);
        joint.Stiffness = stiffness;
        joint.Damping = damping;
        joint.MaxForce = 0f; // Nebula edit

        // Nebula start
        var visuals = EnsureComp<JointVisualsComponent>(target);
        visuals.Sprite = leash.Comp.RopeSprite;
        visuals.Target = user;
        Dirty(target, visuals);
        // Nebula end

        Dirty(target, leashed);
        Dirty(user, puller);
    }

    private bool TryGetHeldLeash(EntityUid user, [NotNullWhen(true)] out Entity<LeashComponent>? leash)
    {
        // Nebula start
        if (TryComp<LeashPullerComponent>(user, out var puller) &&
            _hands.IsHolding(user, puller.Leash) &&
            TryComp<LeashComponent>(puller.Leash, out var current))
        {
            leash = (puller.Leash, current);
            return true;
        }
        // Nebula end

        foreach (var held in _hands.EnumerateHeld(user))
        {
            if (TryComp<LeashComponent>(held, out var comp))
            {
                leash = (held, comp);
                return true;
            }
        }

        leash = null;
        return false;
    }

    #endregion

    #region Detaching

    public void StopAll(Entity<LeashPullerComponent> puller)
    {
        foreach (var target in puller.Comp.Targets.ToArray())
        {
            StopLeash(puller, target);
        }

        RemComp<LeashPullerComponent>(puller);
    }

    public void StopLeash(Entity<LeashPullerComponent> puller, EntityUid target)
    {
        if (!puller.Comp.Targets.Remove(target))
            return;

        if (TryComp<LeashedComponent>(target, out var leashed))
        {
            _joints.RemoveJoint(leashed.Anchor, JointId);
            if (_net.IsServer)
                QueueDel(leashed.Anchor);
        }

        if (TryComp<PhysicsComponent>(target, out var physics))
            _physics.SetSleepingAllowed(target, physics, true);

        RemComp<LeashedComponent>(target);
        RemComp<JointVisualsComponent>(target); // Nebula edit

        if (puller.Comp.Targets.Count == 0)
            RemComp<LeashPullerComponent>(puller);
        else
            Dirty(puller);
    }

    private void OnPullerTerminating(Entity<LeashPullerComponent> ent, ref EntityTerminatingEvent args)
    {
        StopAll(ent);
    }

    private void OnLeashedTerminating(Entity<LeashedComponent> ent, ref EntityTerminatingEvent args)
    {
        if (TryComp<LeashPullerComponent>(ent.Comp.Puller, out var puller))
            StopLeash((ent.Comp.Puller, puller), ent);
    }

    private void OnLeashedInserted(Entity<LeashedComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (TryComp<LeashPullerComponent>(ent.Comp.Puller, out var puller))
            StopLeash((ent.Comp.Puller, puller), ent);
    }

    private void OnLeashedBuckleAttempt(Entity<LeashedComponent> ent, ref BuckleAttemptEvent args)
    {
        args.Cancelled = true;
    }

    #endregion

    #region Leash actions

    private void AdjustDistance(Entity<LeashPullerComponent> puller, float delta)
    {
        if (!TryComp<LeashComponent>(puller.Comp.Leash, out var leash))
            return;

        // Nebula start
        float? changed = null;
        foreach (var target in puller.Comp.Targets)
        {
            if (!TryComp<LeashedComponent>(target, out var leashed))
                continue;

            var next = Math.Clamp(leashed.Distance + delta, leash.MinDistance, leash.MaxDistance);
            if (MathF.Abs(next - leashed.Distance) < 0.001f)
                continue;

            leashed.Distance = next;
            Dirty(target, leashed);
            changed = next;
        }

        if (changed is { } distance)
            _popup.PopupEntity(Loc.GetString("leash-popup-distance", ("distance", MathF.Round(distance, 1))), puller, puller);
        // Nebula end
    }

    private void YankAll(Entity<LeashPullerComponent> puller)
    {
        if (!TryComp<LeashComponent>(puller.Comp.Leash, out var leash) || !_mob.IsAlive(puller))
            return;

        // Nebula start
        if (_timing.CurTime < puller.Comp.NextYank)
        {
            _popup.PopupEntity(Loc.GetString("leash-popup-yank-cooldown"), puller, puller);
            return;
        }

        puller.Comp.NextYank = _timing.CurTime + leash.YankCooldown;
        Dirty(puller);
        // Nebula end

        foreach (var target in puller.Comp.Targets)
        {
            if (!TryComp<LeashedComponent>(target, out var leashed) || !_mob.IsAlive(target))
                continue;

            var range = MathF.Max(YankRange, leashed.Distance);
            if (!_interaction.InRangeUnobstructed(puller.Owner, target, range, popup: true))
                continue;

            var pullerPos = _transform.GetWorldPosition(puller);
            var diff = pullerPos - _transform.GetWorldPosition(target);
            var distance = diff.Length();

            if (distance > 0.001f && _net.IsServer)
                _transform.SetWorldPosition(target, pullerPos - diff / distance * leash.MinDistance);

            _stun.TryKnockdown(target, YankKnockdownTime, refresh: true, autoStand: true, drop: true); // Nebula edit
            _popup.PopupEntity(Loc.GetString("leash-popup-yank"), puller, puller);
            _popup.PopupEntity(Loc.GetString("leash-popup-yanked"), target, target);
        }
    }

    private void PopupSnapped(EntityUid puller, EntityUid target)
    {
        _popup.PopupEntity(Loc.GetString("leash-popup-snapped"), puller, puller);
        _popup.PopupEntity(Loc.GetString("leash-popup-snapped"), target, target);
    }

    private void PopupSnapped(EntityUid puller, LeashPullerComponent comp)
    {
        _popup.PopupEntity(Loc.GetString("leash-popup-snapped"), puller, puller);

        foreach (var target in comp.Targets)
        {
            _popup.PopupEntity(Loc.GetString("leash-popup-snapped"), target, target);
        }
    }

    #endregion
}
