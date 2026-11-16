// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chemistry.Components;
using Content.Shared._Nebula.Chemistry;
using Content.Shared._Nebula.Projectiles;
using Content.Shared.Throwing;
using Content.Trauma.Shared._Nebula.Chemistry;
using Content.Trauma.Shared.Medical.Hypoport;

namespace Content.Trauma.Server._Nebula.Chemistry;

/// <summary>
/// Hardsuit block for chemical projectiles and melee weapons.
/// Syringes and darts don't embed into a hardsuit at all, so an already embedded one keeps injecting.
/// </summary>
public sealed partial class HardsuitInjectOnEventBlockSystem : EntitySystem
{
    [Dependency] private HardsuitInjectionBlockSystem _block = default!;

    [SubscribeLocalEvent]
    private void OnWhileEmbeddedEmbedAttempt(Entity<SolutionInjectWhileEmbeddedComponent> ent, ref EmbedAttemptEvent args)
    {
        TryBlockEmbed(ent, ref args);
    }

    [SubscribeLocalEvent]
    private void OnOnEmbedEmbedAttempt(Entity<SolutionInjectOnEmbedComponent> ent, ref EmbedAttemptEvent args)
    {
        TryBlockEmbed(ent, ref args);
    }

    [SubscribeLocalEvent(before: [typeof(HypoportSystem)])]
    private void OnProjectileHitAttempt(Entity<SolutionInjectOnProjectileHitComponent> ent, ref InjectOnEventAttemptEvent args)
    {
        TryBlockInject(ref args);
    }

    [SubscribeLocalEvent(before: [typeof(HypoportSystem)])]
    private void OnMeleeAttempt(Entity<MeleeChemicalInjectorComponent> ent, ref InjectOnEventAttemptEvent args)
    {
        TryBlockInject(ref args);
    }

    private void TryBlockEmbed(EntityUid projectile, ref EmbedAttemptEvent args)
    {
        // thrown syringes come without a user, the syringe gun throws them too
        var user = args.User ?? CompOrNull<ThrownItemComponent>(projectile)?.Thrower;
        if (_block.IsBlocked(args.Target, user))
            args.Cancelled = true;
    }

    private void TryBlockInject(ref InjectOnEventAttemptEvent args)
    {
        if (_block.IsBlocked(args.Target, args.User))
            args.Cancelled = true;
    }
}
