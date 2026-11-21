// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Medical.Shared.Cybernetics;
using Content.Server.Electrocution;
using Content.Shared._Nebula.Implants;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Nebula.Implants;

/// <summary>
/// Heals the body of an enabled heart implant and hurts it while the implant is disabled by an EMP.
/// </summary>
public sealed partial class CyberHeartImplantSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private ElectrocutionSystem _electrocution = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly TimeSpan ShockInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ShockTime = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan PainPopupInterval = TimeSpan.FromSeconds(5);
    private const float ShockChance = 0.2f;
    private const int ShockDamage = 5;

    [SubscribeLocalEvent]
    private void OnEnabled(Entity<CyberHeartImplantComponent> ent, ref OrganEnabledEvent args)
    {
        ent.Comp.NextHealing = _timing.CurTime + ent.Comp.Interval;
        RemoveEmpSource(ent.Owner, args.Body);
    }

    [SubscribeLocalEvent]
    private void OnDisabled(Entity<CyberHeartImplantComponent> ent, ref OrganDisabledEvent args)
    {
        if (!TryComp<CyberneticsComponent>(ent, out var cybernetics) || !cybernetics.Disabled)
            return;

        var effect = EnsureComp<CyberHeartEmpComponent>(args.Body);
        if (!effect.Sources.Add(ent.Owner) || effect.Sources.Count != 1)
            return;

        effect.NextShock = _timing.CurTime + ShockInterval;
        effect.NextPainPopup = _timing.CurTime;
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<CyberHeartImplantComponent> ent, ref OrganGotRemovedEvent args)
    {
        RemoveEmpSource(ent.Owner, args.Target.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var implants = EntityQueryEnumerator<CyberHeartImplantComponent, EnabledOrganComponent, OrganComponent>();
        while (implants.MoveNext(out var implant, out _, out var organ))
        {
            if (now < implant.NextHealing || organ.Body is not { } body)
                continue;

            implant.NextHealing = now + implant.Interval;
            _damageable.TryChangeDamage(body, implant.Healing, true, false);
        }

        var effects = EntityQueryEnumerator<CyberHeartEmpComponent>();
        while (effects.MoveNext(out var body, out var effect))
        {
            if (now >= effect.NextPainPopup)
            {
                effect.NextPainPopup = now + PainPopupInterval;
                _popup.PopupEntity(Loc.GetString("cyber-heart-implant-emp-pain"), body, body, PopupType.LargeCaution);
            }

            if (now < effect.NextShock)
                continue;

            effect.NextShock = now + ShockInterval;
            if (_random.Prob(ShockChance))
                _electrocution.TryDoElectrocution(body, null, ShockDamage, ShockTime, true, ignoreInsulation: true);
        }
    }

    private void RemoveEmpSource(EntityUid implant, EntityUid body)
    {
        if (!TryComp<CyberHeartEmpComponent>(body, out var effect)
            || !effect.Sources.Remove(implant)
            || effect.Sources.Count != 0)
            return;

        RemCompDeferred<CyberHeartEmpComponent>(body);
    }
}
