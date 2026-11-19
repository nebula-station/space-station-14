// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.Implants;

/// <summary>
/// CNS implant that pulls the body out of a stamina crit with full stamina, then recharges.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CnsRebooterComponent : Component
{
    [DataField(readOnly: true)]
    public TimeSpan Cooldown = TimeSpan.FromMinutes(5);

    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUse;

    /// <summary>
    /// Whether the owner still has to be told that the implant recharged.
    /// </summary>
    [ViewVariables]
    public bool Recharging;
}
