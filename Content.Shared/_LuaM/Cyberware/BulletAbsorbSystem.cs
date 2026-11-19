// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Server/_LuaM/Cyberware/BulletAbsorbSystem.cs

using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Reflect;
using Content.Trauma.Common.Projectiles;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._LuaM.Cyberware;

/// <summary>
/// Lets armor fully absorb one bullet every so often.
/// The hit is predicted by the shooter, so the absorb runs on both sides.
/// </summary>
public sealed partial class BulletAbsorbSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<BulletAbsorbRechargingComponent, BulletAbsorbComponent>();
        while (query.MoveNext(out var uid, out _, out var absorb))
        {
            if (_timing.CurTime < absorb.NextAbsorb)
                continue;

            RemCompDeferred<BulletAbsorbRechargingComponent>(uid);
            _popup.PopupEntity(Loc.GetString(absorb.ReadyPopup), uid, uid);
        }
    }

    [SubscribeLocalEvent]
    private void OnHitByProjectile(Entity<BulletAbsorbComponent> ent, ref GotHitByProjectileEvent args)
    {
        if (args.Cancelled || _timing.CurTime < ent.Comp.NextAbsorb)
            return;

        // Only bullets, energy bolts go through.
        if (TryComp<ReflectiveComponent>(args.Projectile, out var reflective)
            && (reflective.Reflective & ReflectType.NonEnergy) == 0)
            return;

        args.Cancelled = true;
        ent.Comp.NextAbsorb = _timing.CurTime + ent.Comp.Cooldown;
        Dirty(ent);

        if (_net.IsServer)
            EnsureComp<BulletAbsorbRechargingComponent>(ent);

        _audio.PlayPredicted(ent.Comp.AbsorbSound, ent, args.Shooter);
        _popup.PopupPredicted(Loc.GetString(ent.Comp.AbsorbPopup), ent, args.Shooter, PopupType.Medium);
    }
}
