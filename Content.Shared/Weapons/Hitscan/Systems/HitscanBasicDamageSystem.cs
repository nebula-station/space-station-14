// <Trauma>
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
// </Trauma>
using Content.Shared._Nebula.Weapons.Ranged;
using Content.Shared.Damage.Systems;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed partial class HitscanBasicDamageSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    // </Trauma>
    [Dependency] private DamageableSystem _damage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanBasicDamageComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanBasicDamageComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        // <Trauma> - put HitEntity into target
        if (args.Data.HitEntity is not {} target)
            return;

        var dmg = ent.Comp.Damage * _damage.UniversalHitscanDamageModifier;
        // Nebula start
        if (args.Data.Shooter is { } shooter)
        {
            var userEv = new GetUserRangedDamageEvent(target, dmg);
            RaiseLocalEvent(shooter, ref userEv);
            dmg = userEv.Damage;
        }
        // Nebula end

        // <Trauma> - canBeCancelled
        if(!_damage.TryChangeDamage(target, dmg, out var damageDealt, origin: args.Data.Gun, canBeCancelled: true))
            return;
        // </Trauma>

        var damageEvent = new HitscanDamageDealtEvent
        {
            Target = target,
            DamageDealt = damageDealt,
        };
        // </Trauma>

        RaiseLocalEvent(ent, ref damageEvent);
    }
}
