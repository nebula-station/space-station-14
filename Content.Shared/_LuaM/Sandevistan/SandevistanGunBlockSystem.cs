// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Shared/_LuaM/Sandevistan/SandevistanGunBlockSystem.cs

using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Timing;

namespace Content.Shared._LuaM.Sandevistan;

public sealed partial class SandevistanGunBlockSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnShotAttempted(Entity<SandevistanGunBlockComponent> ent, ref ShotAttemptedEvent args)
    {
        args.Cancel();

        if (_timing.CurTime < ent.Comp.NextPopup)
            return;

        ent.Comp.NextPopup = _timing.CurTime + ent.Comp.PopupCooldown;
        _popup.PopupClient(Loc.GetString("sandevistan-no-guns"), ent, ent);
    }
}
