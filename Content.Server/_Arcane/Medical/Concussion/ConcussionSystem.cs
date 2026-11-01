// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/93847a07a4b0b5890003c074e4768c5ba828543a/Content.Arcane.Server/Medical/Concussion/ConcussionSystem.cs

using Content.Shared._Arcane.Medical.Concussion;
using Content.Shared.Alert;
using Content.Shared.Explosion;
using Content.Shared.Mind.Components;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew;
using Content.Trauma.Common.Language.Components;
using Content.Trauma.Common.Language.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Arcane.Medical.Concussion;

public sealed partial class ConcussionSystem : SharedConcussionSystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private StatusEffectsSystem _effects = default!;
    [Dependency] private CommonLanguageSystem _languages = default!;

    private static readonly EntProtoId SpeechEffect = "StatusEffectConcussedSpeech";
    private static readonly ProtoId<AlertPrototype> ConcussionAlert = "Concussion"; // Nebula edit

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var now = Timing.CurTime;
        // Nebula start
        var query = EntityQueryEnumerator<ActiveConcussionComponent, ConcussionThresholdComponent>();
        while (query.MoveNext(out var uid, out _, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;
            // Nebula end

            var elapsed = (now - (comp.NextUpdate - comp.UpdateInterval)).TotalSeconds;
            SetDamage((uid, comp), comp.StoredDamage - Math.Max(0f, comp.HealRate) * elapsed);
            comp.NextUpdate = now + comp.UpdateInterval;
        }
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<ConcussionThresholdComponent> ent, ref MapInitEvent args)
    {
        SetDamage(ent, ent.Comp.StoredDamage);
        ent.Comp.NextUpdate = Timing.CurTime + ent.Comp.UpdateInterval;
    }

    [SubscribeLocalEvent]
    private void OnExplode(Entity<ConcussionThresholdComponent> ent, ref BeforeExplodeEvent args)
    {
        AddConcussionDamage(ent, args.Damage.GetTotal() * ent.Comp.ExplosionDamageMultiplier);
    }

    [SubscribeLocalEvent]
    private void OnFlashbang(Entity<ConcussionThresholdComponent> ent, ref FlashbangConcussionEvent args)
    {
        AddConcussionDamage(ent, args.Damage);
    }

    [SubscribeLocalEvent]
    private void OnStateChanged(Entity<ConcussionThresholdComponent> ent, ref ConcussionStateChangedEvent args)
    {
        if (args.NewState == ConcussionState.Sane)
            ClearEffects(ent);
        else
        {
            EnsureComp<ConcussedComponent>(ent);
            _effects.TrySetStatusEffectDuration(ent, SpeechEffect);
            _alerts.ShowAlert(ent.Owner, ConcussionAlert); // Nebula edit
        }
        RefreshLanguages(ent);
    }

    private void RefreshLanguages(EntityUid body)
    {
        if (TryComp<LanguageSpeakerComponent>(body, out var speaker))
            _languages.UpdateEntityLanguages((body, speaker));
    }

    private void ClearEffects(EntityUid body)
    {
        RemComp<ConcussedComponent>(body);
        _effects.TryRemoveStatusEffect(body, SpeechEffect);
        _alerts.ClearAlert(body, ConcussionAlert); // Nebula edit
    }

    [SubscribeLocalEvent]
    private void OnRejuvenate(Entity<ConcussionThresholdComponent> ent, ref RejuvenateEvent args)
    {
        SetDamage(ent, 0);
    }

    [SubscribeLocalEvent]
    private void OnMindRemoved(Entity<ConcussionThresholdComponent> ent, ref MindRemovedMessage args)
    {
        SetDamage(ent, 0);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<ConcussionThresholdComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.StoredDamage = 0;
        ent.Comp.CurrentState = ConcussionState.Sane;
        if (!TerminatingOrDeleted(ent))
        {
            RemComp<ActiveConcussionComponent>(ent); // Nebula edit
            ClearEffects(ent);
            Movement.RefreshMovementSpeedModifiers(ent.Owner);
            RefreshLanguages(ent);
        }
    }
}
