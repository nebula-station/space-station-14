// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chemistry.Components;
using Content.Shared._Nebula.Chemistry;
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
    private void OnWhileEmbeddedStartup(Entity<SolutionInjectWhileEmbeddedComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<HardsuitBlocksEmbedComponent>(ent);
    }

    [SubscribeLocalEvent]
    private void OnOnEmbedStartup(Entity<SolutionInjectOnEmbedComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<HardsuitBlocksEmbedComponent>(ent);
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

    private void TryBlockInject(ref InjectOnEventAttemptEvent args)
    {
        if (_block.IsBlocked(args.Target, args.User))
            args.Cancelled = true;
    }
}
