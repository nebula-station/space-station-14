// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.ERP.Effects;

/// <summary>
/// Raises arousal by a fixed amount, stopping at a fraction of the maximum.
/// </summary>
public sealed partial class AdjustArousalEntityEffectSystem : EntityEffectSystem<ArousalComponent, AdjustArousal>
{
    [Dependency] private ArousalSystem _arousal = default!;

    protected override void Effect(Entity<ArousalComponent> entity, ref EntityEffectEvent<AdjustArousal> args)
    {
        var cap = entity.Comp.MaxArousal * args.Effect.MaxFraction;
        var current = _arousal.GetArousal(entity.Comp);
        if (current >= cap)
            return;

        _arousal.AddArousal(entity, Math.Min(args.Effect.Amount, cap - current), entity.Comp);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AdjustArousal : EntityEffectBase<AdjustArousal>
{
    [DataField(required: true)]
    public float Amount;

    /// <summary>
    /// Arousal isn't raised past this fraction of the maximum, so 1 can cause an orgasm and anything lower can't.
    /// </summary>
    [DataField]
    public float MaxFraction = 1f;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-adjust-arousal", ("chance", Probability), ("amount", Amount));
}
