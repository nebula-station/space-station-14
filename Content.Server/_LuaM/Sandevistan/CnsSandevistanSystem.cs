// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Server/_Goobstation/Sandevistan/SandevistanSystem.cs

using Content.Server._LuaM.Cyberware;
using Content.Shared._LuaM.Cyberware;
using Content.Shared._LuaM.Sandevistan;
using Content.Shared.Abilities;
using Content.Shared.Actions;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Trauma.Common.Wizard.Projectile;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._LuaM.Sandevistan;

public sealed partial class CnsSandevistanSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan TrailColorInterval = TimeSpan.FromSeconds(0.25);
    private static readonly TimeSpan TrailHuePeriod = TimeSpan.FromSeconds(10.0 / 3);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Recharge needs no ticking, toggling compares RechargeAt with the current time.
        var query = EntityQueryEnumerator<CnsSandevistanActiveComponent, CnsSandevistanUserComponent>();
        while (query.MoveNext(out var uid, out _, out var comp))
        {
            // An overload in this loop can shut it down before the component is culled.
            if (!comp.Active)
                continue;

            if (comp.HeavyOverloadDamage != null
                && comp.HeavyOverloadWarning is { } heavyWarning
                && !comp.HeavyOverloadWarned
                && _timing.CurTime >= comp.ActivatedAt + comp.HeavyOverloadAfter - comp.OverloadWarningTime)
            {
                comp.HeavyOverloadWarned = true;
                _popup.PopupEntity(Loc.GetString(heavyWarning), uid, uid, PopupType.LargeCaution);
                _audio.PlayEntity(comp.OverloadWarningSound, uid, uid);
            }

            if (comp.OverloadWarnings
                && !comp.PreOverloadWarned
                && comp.OverloadAt != null
                && _timing.CurTime >= comp.OverloadAt - comp.OverloadWarningTime)
            {
                comp.PreOverloadWarned = true;
                var left = Math.Max(1, (int) Math.Ceiling((comp.OverloadAt.Value - _timing.CurTime).TotalSeconds));
                _popup.PopupEntity(Loc.GetString("sandevistan-overload-soon", ("time", left)), uid, uid, PopupType.LargeCaution);
                _audio.PlayEntity(comp.OverloadWarningSound, uid, uid);
            }

            if (comp.OverloadAt != null && _timing.CurTime >= comp.OverloadAt)
            {
                if (!comp.OverloadWarned && comp.OverloadWarnings)
                {
                    comp.OverloadWarned = true;
                    _popup.PopupEntity(Loc.GetString("sandevistan-overload"), uid, uid, PopupType.LargeCaution);
                    _audio.PlayEntity(comp.OverloadWarningSound, uid, uid);
                    SetGlitch(uid, comp.OverloadGlitchIntensity);
                }

                if (_timing.CurTime >= comp.NextOverloadDamage)
                {
                    comp.NextOverloadDamage = _timing.CurTime + comp.OverloadInterval;
                    // heavier damage after running for a while, if the implant has it
                    var damage = comp.HeavyOverloadDamage != null && _timing.CurTime >= comp.ActivatedAt + comp.HeavyOverloadAfter
                        ? comp.HeavyOverloadDamage
                        : comp.OverloadDamage;
                    _damageable.TryChangeDamage(uid, damage, ignoreResistances: true, interruptsDoAfters: false);

                    // The damage may have put the user into crit, which shuts the Sandevistan down.
                    if (!comp.Active)
                        continue;
                }
            }

            if (comp.Trail != null && _timing.CurTime >= comp.NextTrailColor)
            {
                comp.NextTrailColor = _timing.CurTime + TrailColorInterval;
                var hue = (float) ((_timing.CurTime - comp.ActivatedAt) / TrailHuePeriod % 1);
                comp.Trail.Color = Color.FromHsv(new System.Numerics.Vector4(hue, 1, 1, 1));
                Dirty(uid, comp.Trail);
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<CnsSandevistanUserComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.ActionUid = _actions.AddAction(ent, ent.Comp.ActionProto);
    }

    // The psycho starts the sandevistan once it is recharged, but never turns it off.
    [SubscribeLocalEvent]
    private void OnPsychosisUse(Entity<CnsSandevistanUserComponent> ent, ref CyberpsychosisUseImplantsEvent args)
    {
        if (ent.Comp.Active)
            return;

        if (ent.Comp.RechargeAt != null)
        {
            if (ent.Comp.RechargeAt > _timing.CurTime)
                return;

            ent.Comp.RechargeAt = null;
        }

        Activate(ent);
    }

    [SubscribeLocalEvent]
    private void OnToggle(Entity<CnsSandevistanUserComponent> ent, ref ToggleCnsSandevistanEvent args)
    {
        args.Handled = true;

        if (ent.Comp.Active)
        {
            _audio.PlayEntity(ent.Comp.EndSound, ent, ent);
            Disable(ent);
            return;
        }

        if (ent.Comp.RechargeAt != null)
        {
            var remaining = ent.Comp.RechargeAt.Value - _timing.CurTime;
            if (remaining > TimeSpan.Zero)
            {
                _popup.PopupEntity(Loc.GetString("sandevistan-recharging", ("time", Math.Ceiling(remaining.TotalSeconds))),
                    ent,
                    ent,
                    PopupType.MediumCaution);
                return;
            }

            ent.Comp.RechargeAt = null;
        }

        Activate(ent);
    }

    private void Activate(Entity<CnsSandevistanUserComponent> ent)
    {
        ent.Comp.Active = true;
        ent.Comp.ActivatedAt = _timing.CurTime;
        ent.Comp.HeavyOverloadWarned = false;
        ent.Comp.OverloadAt = _timing.CurTime + ent.Comp.SafeDuration;
        ent.Comp.NextOverloadDamage = ent.Comp.OverloadAt.Value + ent.Comp.OverloadInterval;
        ent.Comp.OverloadWarned = false;
        ent.Comp.PreOverloadWarned = false;

        if (ent.Comp.BlockGuns)
            EnsureComp<SandevistanGunBlockComponent>(ent);

        SetGlitch(ent, ent.Comp.GlitchIntensity);

        var active = EnsureComp<CnsSandevistanActiveComponent>(ent);
        active.MovementSpeedModifier = ent.Comp.MovementSpeedModifier;
        active.AttackSpeedModifier = ent.Comp.AttackSpeedModifier;
        Dirty(ent, active);

        if (!HasComp<TrailComponent>(ent))
        {
            var trail = AddComp<TrailComponent>(ent);
            trail.RenderedEntity = ent;
            trail.LerpTime = 0.1f;
            trail.LerpDelay = TimeSpan.FromSeconds(4);
            trail.Lifetime = 10;
            trail.Frequency = 0.07f;
            trail.AlphaLerpAmount = 0.2f;
            trail.MaxParticleAmount = 25;
            ent.Comp.Trail = trail;
        }

        if (!HasComp<DogVisionComponent>(ent))
            ent.Comp.Overlay = AddComp<DogVisionComponent>(ent);

        if (_audio.PlayEntity(ent.Comp.StartSound, ent, ent) is { } audio)
            ent.Comp.RunningSound = audio.Entity;
    }

    // A mob state change immediately shuts down an active Sandevistan.
    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<CnsSandevistanUserComponent> ent, ref MobStateChangedEvent args)
    {
        Disable(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CnsSandevistanUserComponent> ent, ref ComponentShutdown args)
    {
        Disable(ent);
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionUid);
    }

    private void Disable(Entity<CnsSandevistanUserComponent> ent)
    {
        if (!ent.Comp.Active)
            return;

        var uid = ent.Owner;
        var comp = ent.Comp;
        comp.Active = false;
        comp.OverloadAt = null;
        RemComp<SandevistanGunBlockComponent>(uid);
        RemComp<CyberpsychosisGlitchComponent>(uid);
        RemComp<CnsSandevistanActiveComponent>(uid);
        comp.RunningSound = _audio.Stop(comp.RunningSound);

        // Start recharge when the Sandevistan shuts down.
        comp.RechargeAt = _timing.CurTime + comp.RechargeDuration;

        if (comp.Overlay != null)
        {
            RemComp<DogVisionComponent>(uid);
            comp.Overlay = null;
        }

        if (comp.Trail != null)
        {
            RemComp<TrailComponent>(uid);
            comp.Trail = null;
        }
    }

    /// <summary>
    /// Shows the glitch screen effect to the user with the given strength.
    /// </summary>
    private void SetGlitch(EntityUid uid, float intensity)
    {
        var glitch = EnsureComp<CyberpsychosisGlitchComponent>(uid);
        if (glitch.Intensity.Equals(intensity))
            return;

        glitch.Intensity = intensity;
        Dirty(uid, glitch);
    }
}
