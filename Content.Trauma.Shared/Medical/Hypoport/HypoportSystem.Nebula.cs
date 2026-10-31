// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Chemistry;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;

namespace Content.Trauma.Shared.Medical.Hypoport;

public sealed partial class HypoportSystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnRequireHypoportAttempt(Entity<RequireHypoportComponent> ent, ref InjectOnEventAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var target = args.Target;
        if (_body.GetOrgan(target, HypoportCategory) is not { } hypoport)
        {
            FailInject(ref args, "hypoport-fail-missing");
            return;
        }

        var ev = new HypoportInjectAttemptEvent(target, args.User ?? ent.Owner, ent.Owner);
        RaiseLocalEvent(hypoport, ref ev);
        if (ev.Cancelled)
            FailInject(ref args, ev.InjectMessageOverride ?? "hypoport-fail-missing");
    }

    private void FailInject(ref InjectOnEventAttemptEvent args, LocId message)
    {
        args.Cancelled = true;
        if (args.User is not { } user)
            return;

        var targetIdent = Identity.Entity(args.Target, EntityManager);
        _popup.PopupEntity(Loc.GetString(message, ("target", targetIdent)), args.Target, user, PopupType.SmallCaution);
    }
}
