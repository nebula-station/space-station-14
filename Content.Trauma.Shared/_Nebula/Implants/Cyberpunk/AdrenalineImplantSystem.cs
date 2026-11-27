// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

public sealed partial class AdrenalineImplantSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private ImplantCooldownSystem _cooldown = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;

    #region Handle events

    [SubscribeLocalEvent]
    private void OnDamageDealt(Entity<AdrenalineHostComponent> ent, ref DamageDealtEvent args)
    {
        var damage = args.ModifiedDamage.GetTotal();
        if (_net.IsClient || damage <= 0)
            return;

        var now = _timing.CurTime;
        foreach (var implant in _body.GetOrgans<AdrenalineImplantComponent>(ent.Owner))
        {
            if (implant.Comp.Charges <= 0
                || now < implant.Comp.NextUse
                || damage <= implant.Comp.Threshold
                || !HasComp<EnabledOrganComponent>(implant))
                continue;

            Inject(implant, ent);
            return;
        }
    }

    [SubscribeLocalEvent]
    private void OnQuery(Entity<AdrenalineImplantComponent> ent, ref ImplantCooldownQueryEvent args)
    {
        args.OnCooldown |= ent.Comp.Charges <= 0 || _timing.CurTime < ent.Comp.NextUse;
    }

    [SubscribeLocalEvent]
    private void OnReduce(Entity<AdrenalineImplantComponent> ent, ref ImplantCooldownReduceEvent args)
    {
        var now = _timing.CurTime;
        if (ent.Comp.NextUse <= now)
            return;

        ent.Comp.NextUse -= (ent.Comp.NextUse - now) * args.Fraction;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnReset(Entity<AdrenalineImplantComponent> ent, ref ImplantCooldownResetEvent args)
    {
        ent.Comp.NextUse = _timing.CurTime;
        ent.Comp.Charges = Math.Max(ent.Comp.Charges, 1);
        Dirty(ent);
    }

    #endregion

    private void Inject(Entity<AdrenalineImplantComponent> implant, EntityUid body)
    {
        if (!_bloodstream.TryAddToBloodstream(body, implant.Comp.Solution.Clone()))
            return;

        implant.Comp.Charges--;
        implant.Comp.NextUse = _timing.CurTime + _cooldown.ModifyCooldown(body, implant, implant.Comp.Cooldown);
        Dirty(implant);

        _popup.PopupEntity(Loc.GetString("adrenaline-implant-popup-inject"), body, body, PopupType.Medium);
    }
}
