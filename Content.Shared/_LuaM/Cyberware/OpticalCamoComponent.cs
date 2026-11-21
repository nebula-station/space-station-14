// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Shared/_LuaM/Cyberware/OpticalCamoComponent.cs

using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._LuaM.Cyberware;

/// <summary>
/// Optical camo implant on the body. The action makes the user fully transparent for a while:
/// faster, and the first attack deals bonus damage. Attacking or taking direct damage breaks it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class OpticalCamoComponent : Component
{
    [DataField]
    public EntProtoId ActionProto = "ActionActivateOpticalCamo";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionUid;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(7);

    /// <summary>
    /// Walk and sprint speed while the camo is on.
    /// </summary>
    [DataField]
    public float SpeedModifier = 1.1f;

    /// <summary>
    /// Damage of the attack that breaks the camo.
    /// </summary>
    [DataField]
    public float FirstHitMultiplier = 1.4f;

    [DataField]
    public SoundSpecifier? ActivateSound;

    [DataField]
    public SoundSpecifier? DeactivateSound;

    [DataField]
    public LocId BrokenPopup = "optical-camo-broken";

    [DataField]
    public LocId BlockedPopup = "optical-camo-blocked";

    [DataField, AutoNetworkedField]
    public bool Active;

    [ViewVariables, AutoPausedField]
    public TimeSpan EndTime;
}

public sealed partial class ActivateOpticalCamoEvent : InstantActionEvent;

/// <summary>
/// Marks a body whose optical camo is on, so only those are checked for the end of the camo.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveOpticalCamoComponent : Component;
