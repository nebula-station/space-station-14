// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Dash;
using Content.Shared._Nebula.Movement.Pulling;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Throwing;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Common.Weapons;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

public sealed partial class GorillaArmsSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private DashActionSystem _dash = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly SoundSpecifier SlamSound = new SoundCollectionSpecifier("MetalSlam");

    private readonly HashSet<Entity<MobStateComponent>> _slamTargets = [];

    [SubscribeLocalEvent(before: [typeof(CyberwarePassivesSystem)])]
    private void OnMeleeDamage(Entity<GorillaArmsComponent> ent, ref GetUserMeleeDamageEvent args)
    {
        if (args.Weapon == ent.Owner)
            args.Damage = new DamageSpecifier(ent.Comp.UnarmedDamage);
    }

    [SubscribeLocalEvent]
    private void OnStartingGrabStage(Entity<GorillaArmsComponent> ent, ref GetStartingGrabStageEvent args)
    {
        var now = _timing.CurTime;
        if (args.Stage != GrabStage.Soft || now < ent.Comp.NextHardGrab)
            return;

        args.Stage = GrabStage.Hard;
        ent.Comp.NextHardGrab = now + ent.Comp.HardGrabCooldown;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnSlamAction(Entity<GorillaSlamComponent> ent, ref DashActionEvent args)
    {
        if (args.Handled || _body.GetBody(ent) is not { } body)
            return;

        if (_dash.TryDash(body, args))
            EnsureComp<GorillaSlammingComponent>(body).Arm = ent;
    }

    [SubscribeLocalEvent]
    private void OnSlamLanded(Entity<GorillaSlammingComponent> ent, ref StopThrowEvent args)
    {
        var arm = ent.Comp.Arm;
        RemCompDeferred<GorillaSlammingComponent>(ent);
        if (_net.IsClient || !TryComp<GorillaSlamComponent>(arm, out var slam))
            return;

        _audio.PlayPvs(SlamSound, ent);

        _slamTargets.Clear();
        _lookup.GetEntitiesInRange(Transform(ent).Coordinates, slam.Range, _slamTargets);
        foreach (var target in _slamTargets)
        {
            if (target.Owner != ent.Owner)
                _damageable.TryChangeDamage(target.Owner, slam.Damage, origin: ent);
        }
    }
}
