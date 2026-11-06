// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Shared.StationRecords;
using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.Hud;

/// <summary>
/// Security neural interface implanted in every security member at round start.
/// Holds the data shown by the HUD, which is visible only while equipment with <see cref="SkewedHudComponent"/> is worn.
/// </summary>
/// <summary>
/// Security module of the neural interface: the squad, its orders and the tasks of the owner.
/// Put next to <see cref="NeuroLinkComponent"/> with the security profile. Fields are sent one by one,
/// so a squad change does not resend the whole wanted list.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class SecurityNeuroComponent : Component
{
    public override bool SendOnlyToOwner => true;

    /// <summary>
    /// Station record of the owner, squads track their members by it. Server only.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public StationRecordKey? Record;

    /// <summary>
    /// The head of security gets the squads summary and loss alerts instead of a squad.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool Commander;

    /// <summary>
    /// Other members of the owner's squad.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public List<SecurityNeuroSquadMember> Squad = [];

    /// <summary>
    /// Active orders of the owner's squad, oldest first.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public List<SecurityNeuroMark> Marks = [];

    /// <summary>
    /// Every task the owner can see: head of security tasks, own personal tasks and the wanted list.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public List<SecurityNeuroTask> Tasks = [];

    /// <summary>
    /// All squads of the station, sent to the commander only.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public List<SecurityNeuroSquadSummary> Summary = [];

    /// <summary>
    /// Bodies the owner's security night vision outlines by health: the squad, or every squad member for the commander.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public List<NetEntity> Allies = [];

    /// <summary>
    /// Tasks written by the owner. Server only, sent through <see cref="Tasks"/>.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public List<SecurityNeuroPersonalTask> Personal = [];

    [ViewVariables(VVAccess.ReadOnly)]
    public int NextPersonalId;

    /// <summary>
    /// When the owner last took enough damage to count as in combat, the squad tablet marks them for a while after.
    /// Server only.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan LastHurt;

    /// <summary>
    /// Who dealt the damage that last set <see cref="LastHurt"/>, the squad tablet marks them on the map.
    /// Server only.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? LastAttacker;

    /// <summary>
    /// When <see cref="LastAttacker"/> was set. Server only.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan LastAttackerAt;

    /// <summary>
    /// Damage taken within the combat window, oldest first. Server only.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public List<(TimeSpan At, FixedPoint2 Damage)> RecentDamage = [];

    /// <summary>
    /// Tasks the owner took off the HUD. Every new task is pinned. Server only.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<SecurityNeuroTaskKey> Unpinned = [];
}
