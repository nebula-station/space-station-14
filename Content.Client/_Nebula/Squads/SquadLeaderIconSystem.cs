// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Overlays;
using Content.Shared._Nebula.Squads;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Nebula.Squads;

/// <summary>
/// Shows the leader icon over squad leaders to anyone who sees the criminal record icons.
/// </summary>
public sealed partial class SquadLeaderIconSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ShowCriminalRecordIconsSystem _criminalIcons = default!;

    private static readonly ProtoId<StatusIconPrototype> LeaderIcon = "SquadLeaderIcon";

    [SubscribeLocalEvent]
    private void OnGetStatusIcons(Entity<SquadLeaderComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_criminalIcons.IsActive && _proto.Resolve(LeaderIcon, out var icon))
            args.StatusIcons.Add(icon);
    }
}
