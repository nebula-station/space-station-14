// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Server/_LuaM/Cyberware/CyberpsychosisComponent.cs

using Content.Server.NPC.HTN;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._LuaM.Cyberware;

/// <summary>
/// Added while a creature is in cyberpsychosis: an NPC controls the body and attacks everyone around.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CyberpsychosisComponent : Component
{
    [ViewVariables, AutoPausedField]
    public TimeSpan EndTime;

    /// <summary>
    /// Mind of the player that lost control, watching through <see cref="Viewer"/>.
    /// </summary>
    [ViewVariables]
    public EntityUid? Mind;

    [ViewVariables]
    public EntityUid? Viewer;

    /// <summary>
    /// Whether the NPC was added by the cyberpsychosis and should be removed after it.
    /// </summary>
    [ViewVariables]
    public bool AddedNpc;

    /// <summary>
    /// Root task of an NPC the creature already had, restored after the cyberpsychosis.
    /// </summary>
    [ViewVariables]
    public HTNCompoundTask? OldRootTask;

    [ViewVariables]
    public HashSet<ProtoId<NpcFactionPrototype>> OldFactions = [];

    /// <summary>
    /// When the psycho tries to turn its implants on again.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan NextImplantUse;

    [DataField]
    public TimeSpan ImplantUseInterval = TimeSpan.FromSeconds(1);
}

/// <summary>
/// Raised on a creature in cyberpsychosis every so often. Implant systems turn their implant on if it is off
/// and ready: the psycho only turns things on, turning them off is left to the player.
/// </summary>
[ByRefEvent]
public record struct CyberpsychosisUseImplantsEvent;
