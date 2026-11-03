// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Events;
using Content.Trauma.Shared.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Systems;

namespace Content.Trauma.Shared._Nebula.MartialArts;

/// <summary>
/// Weapons that block a martial art deal no stamina damage while their user practices it.
/// </summary>
public sealed partial class MartialArtBlockedWeaponSystem : EntitySystem
{
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;

    [SubscribeLocalEvent]
    private void OnStaminaHitAttempt(Entity<KnowledgeGrantOnWearComponent> ent, ref StaminaDamageOnHitAttemptEvent args)
    {
        if (args.User is not { } user
            || _knowledge.GetActiveMartialArt(user) is not { } art
            || Prototype(art) is not { } proto)
            return;

        args.Cancelled |= ent.Comp.Blocked.ContainsKey(proto.ID);
    }
}
