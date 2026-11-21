// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Medical.Shared.Traumas;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Shared._Nebula.Implants;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Traits.Assorted;
using Robust.Server.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Nebula.Implants;

/// <summary>
/// Heals dead bodies with an enabled <see cref="HeartReviverComponent"/> and defibrillates them.
/// </summary>
public sealed partial class HeartReviverSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EuiManager _eui = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedRottingSystem _rotting = default!;
    [Dependency] private TraumaSystem _trauma = default!;

    private static readonly ProtoId<OrganCategoryPrototype> Heart = "Heart";
    private const string HeartModifierId = "HeartReviver";

    [SubscribeLocalEvent]
    private void OnSuicide(Entity<HeartReviverHostComponent> ent, ref SuicideEvent args)
    {
        ent.Comp.Suicided = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<HeartReviverComponent, EnabledOrganComponent, OrganComponent>();
        while (query.MoveNext(out var uid, out var reviver, out _, out var organ))
        {
            if (now < reviver.NextUpdate || organ.Body is not { } body)
                continue;

            reviver.NextUpdate = now + reviver.Interval;
            if (!_mobState.IsDead(body))
            {
                reviver.DeadSince = null;
                continue;
            }

            reviver.DeadSince ??= now;
            if (now < reviver.DeadSince + reviver.StartDelay || !CanRevive(body))
                continue;

            var damage = _mobThreshold.CheckVitalDamage(body);
            if (damage > reviver.MaxDamage)
                continue;

            if (damage > reviver.ReviveDamage)
            {
                _damageable.TryChangeDamage(body, reviver.Healing, true, false,
                    targetPart: TargetBodyPart.All, splitDamage: SplitDamageBehavior.SplitEnsureAll);
                continue;
            }

            Revive((uid, reviver), body);
        }
    }

    private bool CanRevive(EntityUid body)
    {
        return !_rotting.IsRotten(body)
            && !HasComp<UnrevivableComponent>(body)
            && !(TryComp<HeartReviverHostComponent>(body, out var host) && host.Suicided)
            && _body.GetOrgan(body, Heart) != null;
    }

    private void Revive(Entity<HeartReviverComponent> ent, EntityUid body)
    {
        ent.Comp.DeadSince = null;
        _audio.PlayPvs(ent.Comp.ReviveSound, body);
        RestoreBlood(body);
        _mobState.ChangeMobState(body, MobState.Critical);
        _popup.PopupEntity(Loc.GetString("heart-reviver-popup-revive"), body, PopupType.MediumCaution);

        if (_mind.TryGetMind(body, out _, out var mind)
            && mind.CurrentEntity != body
            && _player.TryGetSessionById(mind.UserId, out var session))
            _eui.OpenEui(new ReturnToBodyEui(mind, _mind, _player), session);

        if (_body.GetOrgan(body, Heart) is not { } heart
            || !TryComp<InternalChildOrganComponent>(heart, out var internalOrgan))
            return;

        var cost = internalOrgan.IntegrityCap * ent.Comp.HeartIntegrityCost;
        if (!_trauma.TryChangeOrganDamageModifier((heart, internalOrgan), cost, ent.Owner, HeartModifierId))
            _trauma.TryCreateOrganDamageModifier((heart, internalOrgan), cost, ent.Owner, HeartModifierId);
    }

    /// <summary>
    /// Stops the bleeding and refills the blood, otherwise the body bleeds out again right after the revive.
    /// </summary>
    private void RestoreBlood(EntityUid body)
    {
        if (!TryComp<BloodstreamComponent>(body, out var bloodstream))
            return;

        _bloodstream.TryModifyBleedAmount((body, bloodstream), -bloodstream.BleedAmount);
        _bloodstream.TryModifyBloodLevel((body, bloodstream), bloodstream.BloodReferenceSolution.Volume);
    }
}
