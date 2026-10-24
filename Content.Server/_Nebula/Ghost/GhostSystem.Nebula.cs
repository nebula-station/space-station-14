// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Ghost;
using Content.Shared.Ghost.Systems;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Ghost;

public sealed partial class GhostSystem
{
    /// <summary>
    /// Antagonists recognizable on sight. Hidden ones, even without a job, are never sent to ghosts.
    /// </summary>
    private static readonly HashSet<ProtoId<AntagPrototype>> VisibleAntagonists =
    [
        "Nukeops",
        "NukeopsMedic",
        "NukeopsCommander",
        "Dragon",
        "Wizard",
        "Apprentice",
        "Pirate",
        "Blob",
        "Xenomorph",
        "Xenoborg",
        "MothershipCore",
        "LoneAbductorAntag",
        "AbductorAgentAntag",
        "AbductorScientistAntag",
        "CosmicAntagColossus",
        "Zombie",
        "SpaceNinja",
        "SlaughterDemon",
        "ShadowDemon",
    ];

    /// <summary>
    /// Bodies with a player's mind that <see cref="GetPlayerWarps"/> leaves out: corpses and bodies without a player.
    /// </summary>
    private IEnumerable<GhostWarp> GetBodyWarps(EntityUid? except)
    {
        var query = EntityQueryEnumerator<MindContainerComponent, MobStateComponent>();

        while (query.MoveNext(out var uid, out var mindContainer, out var mobState))
        {
            if (uid == except || mindContainer.Mind is not { } mind)
                continue;

            NebulaGhostWarpState state;

            if (_mobState.IsDead(uid, mobState))
                state = NebulaGhostWarpState.Dead;
            else if (!HasComp<ActorComponent>(uid))
                state = NebulaGhostWarpState.Ssd;
            else
                continue;

            var displayName = $"{Name(uid)} ({_jobs.MindTryGetJobName(mind)})";
            yield return WithNebulaInfo(new GhostWarp(GetNetEntity(uid), displayName, false), uid, mind) with
            {
                State = state,
            };
        }
    }

    /// <summary>
    /// Adds the name, job and visible antagonist flag the Nebula warp window sorts players by.
    /// </summary>
    private GhostWarp WithNebulaInfo(GhostWarp warp, EntityUid attached, EntityUid? mindId)
    {
        _jobs.MindTryGetJobId(mindId, out var job);

        return warp with
        {
            Name = Name(attached),
            Job = job,
            IsVisibleAntagonist = IsVisibleAntagonist(mindId),
        };
    }

    /// <summary>
    /// Whether the mind has a role from <see cref="VisibleAntagonists"/>.
    /// </summary>
    public bool IsVisibleAntagonist(EntityUid? mindId)
    {
        if (!TryComp<MindComponent>(mindId, out var mind))
            return false;

        foreach (var role in mind.MindRoleContainer.ContainedEntities)
        {
            if (TryComp<MindRoleComponent>(role, out var comp)
                && comp.AntagPrototype is { } antag
                && VisibleAntagonists.Contains(antag))
            {
                return true;
            }
        }

        return false;
    }
}
