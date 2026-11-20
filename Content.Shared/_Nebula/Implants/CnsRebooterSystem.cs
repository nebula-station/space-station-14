// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Shared._Nebula.Damage;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Nebula.Implants;

/// <summary>
/// Reboots the nervous system of a body with an enabled <see cref="CnsRebooterComponent"/> once it enters a stamina crit.
/// </summary>
public sealed partial class CnsRebooterSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private StatusEffectsSystem _status = default!;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(0.2);

    private TimeSpan _nextCheck;

    [SubscribeLocalEvent]
    private void OnBodyStamCrit(Entity<BodyComponent> ent, ref StaminaCritEnteredEvent args)
    {
        _body.RefRelayBodyEvent(ent, ent.Comp, ref args);
    }

    [SubscribeLocalEvent]
    private void OnStamCrit(Entity<CnsRebooterComponent> ent, ref BodyRelayedEvent<StaminaCritEnteredEvent> args)
    {
        var body = args.Body.Owner;
        if (!HasComp<EnabledOrganComponent>(ent)
            || _timing.CurTime < ent.Comp.NextUse
            || !TryComp<StaminaComponent>(body, out var stamina))
            return;

        ent.Comp.NextUse = _timing.CurTime + ent.Comp.Cooldown;
        ent.Comp.Recharging = true;
        Dirty(ent);

        _stamina.ExitStamCrit(body, stamina);
        _stamina.TakeStaminaDamage(body, -stamina.StaminaDamage, stamina, visual: false, ignoreResist: true);
        _status.TryRemoveStatusEffect(body, SharedStunSystem.StunId);
        _stun.ForceStandUp(body);

        _popup.PopupEntity(Loc.GetString("cns-rebooter-popup-reboot"), body, body, PopupType.Medium);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (_net.IsClient || now < _nextCheck)
            return;

        _nextCheck = now + CheckInterval;

        var query = EntityQueryEnumerator<CnsRebooterComponent, OrganComponent>();
        while (query.MoveNext(out var uid, out var rebooter, out var organ))
        {
            if (!rebooter.Recharging || now < rebooter.NextUse)
                continue;

            rebooter.Recharging = false;
            if (organ.Body is { } body)
                _popup.PopupEntity(Loc.GetString("cns-rebooter-popup-ready"), body, body);
        }
    }
}
