// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Events;
using Content.Shared.Timing.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._Nebula.Security.StunDelayGroup;

public sealed partial class StunDelayGroupSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private UseDelaySystem _delay = default!;

    [SubscribeLocalEvent]
    private void OnHit(Entity<StunDelayGroupComponent> ent, ref StaminaMeleeHitEvent args)
    {
        if (args.HitList.Count == 0
            || !_container.TryGetContainingContainer(ent.Owner, out var container)
            || !_delay.TryGetDelayInfo(ent.Owner, out var info, ent.Comp.UseDelay))
            return;

        var user = EnsureComp<StunDelayUserComponent>(container.Owner);
        user.NextStun = _timing.CurTime + info.Length;
        Dirty(container.Owner, user);

        _delay.TryResetDelay(ent.Owner, id: ent.Comp.UseDelay);
    }

    [SubscribeLocalEvent]
    private void OnMeleeAttempt(Entity<StunDelayGroupComponent> ent, ref AttemptMeleeEvent args)
    {
        if (TryComp<StunDelayUserComponent>(args.User, out var user)
            && _timing.CurTime < user.NextStun)
            args.Cancelled = true;
    }
}
