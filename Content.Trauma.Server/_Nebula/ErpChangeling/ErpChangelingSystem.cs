// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Changeling;
using Content.Goobstation.Shared.Changeling.Components;
using Content.Server._Nebula.ERP;
using Content.Server.Mind;
using Content.Server.Objectives.Systems;
using Content.Server.Polymorph.Systems;
using Content.Server.Store.Systems;
using Content.Shared._Nebula.Changeling;
using Content.Shared._Nebula.ERP;
using Content.Shared._Nebula.ErpPanel;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Trauma.Server._Nebula.ErpChangeling.Objectives;
using Content.Trauma.Shared._Nebula.ErpChangeling;
using Robust.Shared.Timing;

namespace Content.Trauma.Server._Nebula.ErpChangeling;

public sealed partial class ErpChangelingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ChangelingSystem _changeling = default!;
    [Dependency] private ErpConsentSystem _consent = default!;
    [Dependency] private ErpChangelingObjectivesSystem _objectives = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StoreSystem _store = default!;
    [Dependency] private TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ErpChangelingComponent, ChangelingStingAttemptEvent>(OnStingAttempt);
        SubscribeLocalEvent<ErpChangelingComponent, ErpPanelInteractionAttemptEvent>(OnPanelInteractionAttempt);
        SubscribeLocalEvent<ErpChangelingComponent, PolymorphedEvent>(OnPolymorphed);
        SubscribeLocalEvent<ErpRecentPartnerComponent, ArousalOrgasmEvent>(OnPartnerOrgasm);
    }

    private void OnStingAttempt(Entity<ErpChangelingComponent> ent, ref ChangelingStingAttemptEvent args)
    {
        var target = args.Target;

        if (!Consents(ent, target))
        {
            args.Cancelled = true;
            return;
        }

        if (!HasComp<ErpChangelingStingComponent>(args.Action) || !_mind.TryGetMind(ent, out _, out var mind))
            return;

        foreach (var objective in mind.Objectives)
        {
            if (TryComp<ErpArouseCrewConditionComponent>(objective, out var condition))
                condition.Targets.Add(target);
        }
    }

    // transforming makes a new body, without this it would be a normal changeling afterwards
    private void OnPolymorphed(Entity<ErpChangelingComponent> ent, ref PolymorphedEvent args)
    {
        _polymorph.CopyPolymorphComponent<ErpChangelingComponent>(ent, args.NewEntity);
    }

    private void OnPanelInteractionAttempt(Entity<ErpChangelingComponent> ent, ref ErpPanelInteractionAttemptEvent args)
    {
        if (args.Erotic && !Consents(ent, args.Target))
            args.Cancelled = true;
    }

    /// <summary>
    /// Only characters who agreed to be horny antag victims can be stung or ERPed with, shows a popup if not.
    /// Characters without a profile have no consent tags and are always fine.
    /// </summary>
    private bool Consents(EntityUid ling, EntityUid target)
    {
        if (!HasComp<HumanoidProfileComponent>(target)
            || _consent.GetConsent(target, ErpConsentSystem.HornyAntagVictim) == ErpConsent.Yes)
            return true;

        _popup.PopupEntity(Loc.GetString("erp-changeling-fail-consent",
            ("target", Identity.Entity(target, EntityManager))), ling, ling);
        return false;
    }

    private void OnPartnerOrgasm(Entity<ErpRecentPartnerComponent> ent, ref ArousalOrgasmEvent args)
    {
        var ling = ent.Comp.Partner;
        if (!TryComp<ErpChangelingComponent>(ling, out var erpLing)
            || _timing.CurTime > ent.Comp.Time + erpLing.PartnerWindow)
            return;

        // minds so a partner can't be fed on again after switching bodies, bodies for NPCs
        var hasMind = _mind.TryGetMind(ent, out var partnerMind, out _);
        if (erpLing.RewardedPartners.Add(hasMind ? partnerMind : ent.Owner))
            Feed((ling, erpLing), ent);

        if (!hasMind || !_mind.TryGetMind(ling, out _, out var lingMind))
            return;

        foreach (var objective in lingMind.Objectives)
        {
            if (HasComp<ErpOrgasmConditionComponent>(objective)
                && _target.GetTarget(objective, out var target)
                && target == partnerMind)
                _objectives.CompleteOrgasm(objective);
        }
    }

    private void Feed(Entity<ErpChangelingComponent> ling, EntityUid partner)
    {
        if (TryComp<ChangelingIdentityComponent>(ling, out var identity))
            identity.TotalEvolutionPoints += ling.Comp.EvolutionPointsPerPartner;

        if (_changeling.GetStore(ling) is { } store)
        {
            _store.TryAddCurrency(new() { { "EvolutionPoint", ling.Comp.EvolutionPointsPerPartner } }, store.Owner, store.Comp);
            _store.UpdateUserInterface(ling, store.Owner, store.Comp);
        }

        _popup.PopupEntity(Loc.GetString("erp-changeling-fed",
            ("target", Identity.Entity(partner, EntityManager)),
            ("points", ling.Comp.EvolutionPointsPerPartner)), ling, ling);
    }
}
