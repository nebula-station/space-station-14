// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Targeting;
using Content.Medical.Shared.Traumas;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Mobs.Systems;

namespace Content.Trauma.Shared._Nebula.MartialArts;

/// <summary>
/// Breaks the head bone and deals enough damage to kill.
/// </summary>
public sealed partial class SnapNeck : EntityEffectBase<SnapNeck>
{
    [DataField]
    public ProtoId<OrganCategoryPrototype> Category = "Head";

    [DataField]
    public ProtoId<DamageTypePrototype> DamageType = "Blunt";

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class SnapNeckEffectSystem : EntityEffectSystem<BodyComponent, SnapNeck>
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private TraumaSystem _trauma = default!;
    [Dependency] private WoundSystem _wound = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<SnapNeck> args)
    {
        if (_body.GetOrgan(ent, args.Effect.Category) is { } head)
            BreakBone(head);

        if (!_threshold.TryGetDeadThreshold(ent, out var threshold))
            return;

        var damage = new DamageSpecifier(_proto.Index(args.Effect.DamageType), threshold.Value);

        // Aimed at the chest: this much damage on the head would sever it.
        _damageable.TryChangeDamage(ent.Owner,
            damage,
            ignoreResistances: true,
            interruptsDoAfters: false,
            origin: args.User,
            targetPart: TargetBodyPart.Chest);
    }

    private void BreakBone(EntityUid part)
    {
        // Slimes and IPCs have heads without bones.
        if (!TryComp<BoneComponent>(part, out var bone)
            || !_wound.TryCreateWound(part, TraumaSystem.Blunt.Id, 0, out var wound, TraumaSystem.Brute))
            return;

        // The empty wound only holds the trauma, bone surgery isn't offered without one.
        var inflicter = EnsureComp<TraumaInflicterComponent>(wound.Value);
        _trauma.ApplyBoneTrauma((part, bone), (wound.Value, inflicter), bone.IntegrityCap);
    }
}
