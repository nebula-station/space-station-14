// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Popups;
using Content.Server.Preferences.Managers;
using Content.Shared._Nebula.Ghost.Roles.CopyProfile;
using Content.Shared.Body;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Station.Systems;
using Content.Trauma.Common.Ghost;
using Robust.Shared.Prototypes;

namespace Content.Server._Nebula.Ghost.Roles.CopyProfile;

public sealed partial class GhostRoleCopyProfileSystem : EntitySystem
{
    [Dependency] private IServerPreferencesManager _prefs = default!;
    [Dependency] private HumanoidProfileSystem _humanoid = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private StationSpawningSystem _spawning = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;

    [SubscribeLocalEvent]
    private void OnTakeAttempt(Entity<GhostRoleCopyProfileComponent> ent, ref GhostRoleTakeAttemptEvent args)
    {
        var prefs = _prefs.GetPreferences(args.Player.UserId);

        if (ent.Comp.Used.Contains((args.Player.UserId, prefs.SelectedCharacterIndex)))
        {
            _popup.PopupCursor(Loc.GetString("ghost-role-copy-already-used"), args.Player);
            args.Cancelled = true;
            return;
        }

        if (prefs.SelectedCharacter is HumanoidCharacterProfile profile &&
            TryComp<GhostRoleMobSpawnerComponent>(ent, out var spawner) &&
            _proto.TryIndex(profile.Species, out var species))
        {
            spawner.Prototype = species.Prototype;
        }
    }

    [SubscribeLocalEvent]
    private void OnMindCreated(Entity<GhostRoleCopyProfileComponent> ent, ref GhostRoleCreatedMindEvent args)
    {
        if (!TryComp<MindComponent>(args.Mind, out var mind) || mind.UserId is not { } userId)
            return;

        var prefs = _prefs.GetPreferences(userId);

        ent.Comp.Used.Add((userId, prefs.SelectedCharacterIndex));

        if (prefs.SelectedCharacter is HumanoidCharacterProfile profile)
        {
            _visualBody.ApplyProfileTo(args.Mob, profile);
            _humanoid.ApplyProfileTo(args.Mob, profile);
            _meta.SetEntityName(args.Mob, profile.Name);
        }

        if (ent.Comp.Gear is { } gear)
            _spawning.EquipStartingGear(args.Mob, gear);
    }
}
