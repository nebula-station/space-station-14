// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Shared/_LuaM/MantisBlades/MantisBladeComboComponent.cs

using Robust.Shared.Prototypes;

namespace Content.Shared._LuaM.MantisBlades;

/// <summary>
/// Mantis blade that unlocks extra abilities while enough blades are extended at once:
/// a dash action for the user and reflection on the blades.
/// </summary>
[RegisterComponent]
public sealed partial class MantisBladeComboComponent : Component
{
    /// <summary>
    /// How many blades have to be held for the combo to be active.
    /// </summary>
    [DataField]
    public int RequiredBlades = 2;

    /// <summary>
    /// Reflect chance of the blade while the combo is active. It is 0 otherwise.
    /// </summary>
    [DataField]
    public float ReflectProb = 0.35f;

    /// <summary>
    /// Action given to the user while the combo is active.
    /// </summary>
    [DataField]
    public EntProtoId DashAction = "ActionMantisBladeDash";
}

/// <summary>
/// Keeps the combo dash action of a mantis blade user and its cooldown between blade extensions.
/// </summary>
[RegisterComponent]
public sealed partial class MantisBladeComboUserComponent : Component
{
    [ViewVariables]
    public EntityUid? DashActionUid;

    /// <summary>
    /// Cooldown of the dash, restored when the action is given again so retracting the blades doesn't reset it.
    /// </summary>
    [ViewVariables]
    public (TimeSpan Start, TimeSpan End)? DashCooldown;
}
