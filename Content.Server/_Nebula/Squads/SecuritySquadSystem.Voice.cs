// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Hud;
using Content.Shared._Nebula.Squads;
using Content.Shared.Radio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Nebula.Squads;

public sealed partial class SecuritySquadSystem
{
    private static readonly ProtoId<RadioChannelPrototype> SecurityChannel = "Security";

    /// <summary>
    /// Plays the voice command into the security headsets of the squad members.
    /// A squad gets one command per <see cref="SquadLimits.VoiceCommandCooldown"/>.
    /// </summary>
    public bool TryPlayVoiceCommand(EntityUid station, int squadId, ProtoId<SquadVoiceCommandPrototype> command)
    {
        if (!TryComp<StationSquadsComponent>(station, out var squads)
            || squads.Squads.Find(s => s.Id == squadId) is not { } squad
            || !_proto.Resolve(command, out var proto)
            || _timing.CurTime < squad.NextVoiceCommand)
            return false;

        var mobs = GetMobs(station);
        var listeners = Filter.Empty();
        foreach (var key in squad.Members)
        {
            if (!mobs.TryGetValue(key, out var mob)
                || HasComp<NeuroSignalLostComponent>(mob)
                || !_radioSound.WearsHeadset(mob, SecurityChannel)
                || !_player.TryGetSessionByEntity(mob, out var session))
                continue;

            listeners.AddPlayer(session);
        }

        if (listeners.Count == 0)
            return false;

        _audio.PlayGlobal(proto.Sound, listeners, true);
        squad.NextVoiceCommand = _timing.CurTime + SquadLimits.VoiceCommandCooldown;
        UpdateTablets(station);
        return true;
    }
}
