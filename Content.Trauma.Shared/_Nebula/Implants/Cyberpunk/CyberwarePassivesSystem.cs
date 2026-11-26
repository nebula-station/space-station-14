// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Targeting;
using Content.Medical.Shared.Cybernetics;
using Content.Shared._LuaM.Cyberware;
using Content.Shared._Nebula.Weapons.Ranged;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Content.Trauma.Common.Weapons;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

/// <summary>
/// Passive effects that cyberware puts on its owner.
/// </summary>
public sealed partial class CyberwarePassivesSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;

    private static readonly TimeSpan RegenInterval = TimeSpan.FromSeconds(1);

    private TimeSpan _nextRegen;

    #region Damage dealt

    [SubscribeLocalEvent]
    private void OnRangedDistance(Entity<RangedDistanceBonusComponent> ent, ref GetUserRangedDamageEvent args)
    {
        var distance = (_transform.GetWorldPosition(args.Target) - _transform.GetWorldPosition(ent)).Length();
        args.Damage *= 1 + MathF.Min(distance * ent.Comp.BonusPerTile, ent.Comp.MaxBonus);
    }

    [SubscribeLocalEvent]
    private void OnRangedHeadshot(Entity<HeadshotBonusComponent> ent, ref GetUserRangedDamageEvent args)
    {
        if (IsAimingAtHead(ent))
            args.Damage *= 1 + ent.Comp.Bonus;
    }

    [SubscribeLocalEvent]
    private void OnMeleeHeadshot(Entity<HeadshotBonusComponent> ent, ref GetUserMeleeDamageEvent args)
    {
        if (IsAimingAtHead(ent))
            args.Damage *= 1 + ent.Comp.Bonus;
    }

    #endregion

    #region Damage taken

    [SubscribeLocalEvent]
    private void OnInverterDamageModify(Entity<PeripheralInverterComponent> ent, ref DamageModifyEvent args)
    {
        if (args.Origin is not { } origin
            || origin == ent.Owner
            || TerminatingOrDeleted(origin))
            return;

        var distance = (_transform.GetWorldPosition(origin) - _transform.GetWorldPosition(ent)).Length();
        var multiplier = distance <= ent.Comp.Range ? ent.Comp.NearMultiplier : ent.Comp.FarMultiplier;
        args.Damage = ScaleDamage(args.Damage, multiplier);
    }

    [SubscribeLocalEvent]
    private void OnArmorBoostDamageModify(Entity<DermalArmorBoostComponent> ent, ref DamageModifyEvent args)
    {
        if (!TryComp<SubdermalArmorComponent>(ent, out var armor))
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, GetBoost(armor.Modifiers, ent.Comp.Multiplier));
    }

    [SubscribeLocalEvent]
    private void OnCyberneticsEmpAttempt(Entity<CyberneticsComponent> ent, ref EmpAttemptEvent args)
    {
        if (_body.GetBody(ent) is { } body && HasComp<EmpCoatingComponent>(body))
            args.Cancelled = true;
    }

    #endregion

    #region Stamina

    [SubscribeLocalEvent]
    private void OnStaminaBonusStartup(Entity<StaminaBonusComponent> ent, ref ComponentStartup args)
    {
        _stamina.RefreshStaminaCritThreshold(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnStaminaBonusShutdown(Entity<StaminaBonusComponent> ent, ref ComponentShutdown args)
    {
        _stamina.RefreshStaminaCritThreshold(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnRefreshStamina(Entity<StaminaBonusComponent> ent, ref RefreshStaminaCritThresholdEvent args)
    {
        if (ent.Comp.LifeStage < ComponentLifeStage.Stopping)
            args.ThresholdValue += ent.Comp.Bonus;
    }

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (_net.IsClient || now < _nextRegen)
            return;

        _nextRegen = now + RegenInterval;

        var query = EntityQueryEnumerator<ChitinRegenerationComponent, DamageableComponent, BodyComponent>();
        while (query.MoveNext(out var uid, out var chitin, out var damageable, out var body))
        {
            Regenerate((uid, chitin, damageable, body));
        }
    }

    private void Regenerate(Entity<ChitinRegenerationComponent, DamageableComponent, BodyComponent> ent)
    {
        var (uid, chitin, damageable, body) = ent;
        if (_damageable.GetTotalDamage((uid, damageable)) >= chitin.Threshold
            || !_damageable.GetDamagePerGroup((uid, damageable)).TryGetValue(chitin.Group, out var total)
            || total <= 0)
            return;

        var heal = chitin.HealPerSecond * (float) RegenInterval.TotalSeconds;
        foreach (var part in _body.GetExternalOrgans((uid, body)))
        {
            if (!TryComp<DamageableComponent>(part, out var partDamage)
                || !_damageable.GetDamagePerGroup((part.Owner, partDamage)).TryGetValue(chitin.Group, out var partTotal)
                || partTotal <= 0)
                continue;

            _damageable.HealDistributed((part.Owner, partDamage), -heal * partTotal / total, chitin.Group);
        }
    }

    private bool IsAimingAtHead(EntityUid uid)
    {
        return TryComp<TargetingComponent>(uid, out var targeting) && targeting.Target == TargetBodyPart.Head;
    }

    /// <summary>
    /// Scales only the damage, leaving healing as it is.
    /// </summary>
    private static DamageSpecifier ScaleDamage(DamageSpecifier damage, float multiplier)
    {
        var scaled = new DamageSpecifier(damage);
        foreach (var (type, value) in damage.DamageDict)
        {
            if (value > 0)
                scaled.DamageDict[type] = value * multiplier;
        }

        return scaled;
    }

    /// <summary>
    /// Extra modifiers that make the armor protect the given times more.
    /// </summary>
    private static DamageModifierSet GetBoost(DamageModifierSet armor, float multiplier)
    {
        var boost = new DamageModifierSet();
        foreach (var (type, coefficient) in armor.Coefficients)
        {
            if (coefficient is < 1f and > 0f)
                boost.Coefficients[type] = MathF.Max(0f, 1f - multiplier * (1f - coefficient)) / coefficient;
        }

        foreach (var (type, flat) in armor.FlatReductions)
        {
            if (flat > 0f)
                boost.FlatReductions[type] = flat * (multiplier - 1f);
        }

        return boost;
    }
}
