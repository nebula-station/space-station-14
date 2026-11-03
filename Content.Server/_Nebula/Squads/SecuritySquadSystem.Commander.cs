// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Nebula.Hud;
using Content.Shared.Mobs;
using Content.Shared.StationRecords;

namespace Content.Server._Nebula.Squads;

public sealed partial class SecuritySquadSystem
{
    /// <summary>
    /// Sends the commanders of the station a summary line per squad, when it changed.
    /// </summary>
    /// <param name="mobs">Bodies already collected by the caller, collected here when null.</param>
    private void RefreshSummary(Entity<StationSquadsComponent> squads,
        Dictionary<StationRecordKey, Entity<SecurityNeuroComponent>>? mobs = null)
    {
        mobs ??= GetMobs(squads);
        List<SecurityNeuroSquadSummary> summary = [];
        foreach (var squad in squads.Comp.Squads)
        {
            var alive = 0;
            foreach (var key in squad.Members)
            {
                if (GetState(mobs, key) == MobState.Alive)
                    alive++;
            }

            summary.Add(new SecurityNeuroSquadSummary
            {
                Name = squad.Name,
                Color = squad.Color,
                Alive = alive,
                Total = squad.Members.Count,
                Losses = alive < squad.Members.Count,
            });
        }

        List<NetEntity> allies = [];
        foreach (var squad in squads.Comp.Squads)
        {
            foreach (var key in squad.Members)
            {
                if (mobs.TryGetValue(key, out var body))
                    allies.Add(GetNetEntity(body));
            }
        }

        foreach (var mob in mobs.Values)
        {
            if (!mob.Comp.Commander
                || HasComp<NeuroSignalLostComponent>(mob))
                continue;

            var self = GetNetEntity(mob);
            SetAllies(mob, allies.FindAll(ally => ally != self));
            if (summary.SequenceEqual(mob.Comp.Summary))
                continue;

            mob.Comp.Summary = [.. summary];
            DirtyField(mob, mob.Comp, nameof(SecurityNeuroComponent.Summary));
        }
    }

    /// <summary>
    /// Replaces the outlined bodies of the member, when they changed.
    /// </summary>
    private void SetAllies(Entity<SecurityNeuroComponent> mob, List<NetEntity> allies)
    {
        if (allies.SequenceEqual(mob.Comp.Allies))
            return;

        mob.Comp.Allies = [.. allies];
        DirtyField(mob, mob.Comp, nameof(SecurityNeuroComponent.Allies));
    }

    private void AlertCommanders(Entity<StationSquadsComponent> squads, SecuritySquad squad, StationRecordKey member, MobState state)
    {
        if (!_records.TryGetRecord<GeneralStationRecord>(member, out var record))
            return;

        var ev = new SecurityNeuroLossEvent(record.Name, squad.Name, state);
        foreach (var mob in GetMobs(squads).Values)
        {
            if (mob.Comp.Commander
                && !HasComp<NeuroSignalLostComponent>(mob)
                && _player.TryGetSessionByEntity(mob, out var session))
                RaiseNetworkEvent(ev, session);
        }
    }
}
