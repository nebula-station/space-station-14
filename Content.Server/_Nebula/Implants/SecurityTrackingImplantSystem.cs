// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Nebula.Radio;
using Content.Server.Administration.Logs;
using Content.Server.Pinpointer;
using Content.Shared._Nebula.Hud;
using Content.Shared._Nebula.Implants;
using Content.Shared.Database;
using Content.Shared.Implants;
using Content.Shared.Mobs;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Nebula.Implants;

public sealed partial class SecurityTrackingImplantSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private RadioSoundSystem _radioSound = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<SecurityTrackingImplantComponent> ent, ref ImplantRelayEvent<MobStateChangedEvent> args)
    {
        if (args.Args.NewMobState is not (MobState.SoftCrit or MobState.Critical)
            || args.Args.OldMobState != MobState.Alive
            || _timing.CurTime < ent.Comp.NextAlert)
            return;

        var mob = args.ImplantedEntity;
        ent.Comp.NextAlert = _timing.CurTime + ent.Comp.Cooldown;

        _audio.PlayEntity(ent.Comp.FlatlineSound,
            Filter.PvsExcept(mob),
            mob,
            true,
            AudioParams.Default.WithMaxDistance(ent.Comp.FlatlineRange));

        var position = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(mob));
        var message = Loc.GetString(ent.Comp.Message, ("user", mob), ("position", position));
        _radioSound.SendRadioMessage(ent.Owner, message, ent.Comp.Channel, ent.Owner, ent.Comp.AlertSound, mob);

        var location = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(mob, onlyName: true));
        var mark = new SecurityImplantCritMarkEvent(GetNetCoordinates(_transform.GetMoverCoordinates(mob)),
            ent.Comp.MarkDuration,
            Name(mob),
            location);
        var query = EntityQueryEnumerator<NeuroLinkComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (!HasComp<NeuroSignalLostComponent>(uid) && _player.TryGetSessionByEntity(uid, out var session))
                RaiseNetworkEvent(mark, session);
        }

        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"Security tracking implant of {ToPrettyString(mob):player} raised a crit alert");
    }
}
