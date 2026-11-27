// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Abductor;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Forensics;
using Content.Shared.Forensics.Components;
using Content.Shared.Forensics.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Content.Trauma.Server.Abductor;
using Content.Trauma.Shared._Nebula.Implants.Cyberpunk;
using Robust.Shared.Timing;

namespace Content.Trauma.Server._Nebula.Implants;

/// <summary>
/// Puts on and takes off the random identity of a <see cref="FacePanelImplantComponent"/>.
/// </summary>
public sealed partial class FacePanelImplantSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private ImplantCooldownSystem _cooldown = default!;
    [Dependency] private AbductorVestDisguiseSystem _disguise = default!;
    [Dependency] private ForensicsSystem _forensics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);

    private TimeSpan _nextCheck;

    [SubscribeLocalEvent]
    private void OnAction(Entity<FacePanelImplantComponent> ent, ref FacePanelActionEvent args)
    {
        if (args.Handled || _body.GetBody(ent) is not { } body)
            return;

        args.Handled = true;
        if (ent.Comp.DisguiseEnd == null)
        {
            _actions.SetUseDelay(args.Action.AsNullable(), null);
            PutOn(ent, body);
            return;
        }

        // PerformAction clears the cooldown after this handler, so a manual take-off recharges through the use delay
        _actions.SetUseDelay(args.Action.AsNullable(), ent.Comp.Cooldown);
        TakeOff(ent, body, startCooldown: false);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<FacePanelImplantComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (ent.Comp.DisguiseEnd != null && !TerminatingOrDeleted(args.Target))
            TakeOff(ent, args.Target);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (now < _nextCheck)
            return;

        _nextCheck = now + CheckInterval;

        var query = EntityQueryEnumerator<FacePanelImplantComponent, OrganComponent>();
        while (query.MoveNext(out var uid, out var panel, out var organ))
        {
            if (panel.DisguiseEnd is { } end && now >= end && organ.Body is { } body)
                TakeOff((uid, panel), body);
        }
    }

    private void PutOn(Entity<FacePanelImplantComponent> ent, EntityUid body)
    {
        if (!TryComp<HumanoidProfileComponent>(body, out var humanoid)
            || HasComp<AbductorDisguiseStateComponent>(body))
        {
            _popup.PopupEntity(Loc.GetString("face-panel-popup-failed"), body, body, PopupType.SmallCaution);
            return;
        }

        var profile = HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species);
        _disguise.ApplyDisguise(body, profile, raiseRenameEvents: false);

        ent.Comp.OriginalDna = CompOrNull<DnaComponent>(body)?.DNA;
        ent.Comp.OriginalFingerprint = CompOrNull<FingerprintComponent>(body)?.Fingerprint;
        _forensics.RandomizeDNA(body);
        _forensics.RandomizeFingerprint(body);

        ent.Comp.DisguiseEnd = _timing.CurTime + ent.Comp.Duration;
        _popup.PopupEntity(Loc.GetString("face-panel-popup-on"), body, body);
    }

    private void TakeOff(Entity<FacePanelImplantComponent> ent, EntityUid body, bool startCooldown = true)
    {
        ent.Comp.DisguiseEnd = null;
        _disguise.RestoreAppearance(body, raiseRenameEvents: false);

        if (TryComp<DnaComponent>(body, out var dna) && ent.Comp.OriginalDna is { } originalDna)
        {
            dna.DNA = originalDna;
            var ev = new GenerateDnaEvent { Owner = body, DNA = originalDna };
            RaiseLocalEvent(body, ref ev);
            Dirty(body, dna);
        }

        if (TryComp<FingerprintComponent>(body, out var fingerprint) && ent.Comp.OriginalFingerprint is { } originalFingerprint)
        {
            fingerprint.Fingerprint = originalFingerprint;
            Dirty(body, fingerprint);
        }

        if (startCooldown)
            _cooldown.StartCooldown(ent, _cooldown.ModifyCooldown(body, ent, ent.Comp.Cooldown));

        _popup.PopupEntity(Loc.GetString("face-panel-popup-off"), body, body);
    }
}
