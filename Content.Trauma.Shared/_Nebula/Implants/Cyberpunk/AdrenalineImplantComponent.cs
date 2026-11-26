// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

/// <summary>
/// Torso implant that injects its solution when its owner takes a heavy hit, a few times in total.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class AdrenalineImplantComponent : Component
{
    /// <summary>
    /// A single hit has to deal more damage than this.
    /// </summary>
    [DataField(readOnly: true)]
    public FixedPoint2 Threshold = 10;

    [DataField(readOnly: true)]
    public TimeSpan Cooldown = TimeSpan.FromMinutes(5);

    [DataField(readOnly: true)]
    public Solution Solution = new();

    [DataField, AutoNetworkedField]
    public int Charges = 3;

    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUse;
}

/// <summary>
/// Body of an <see cref="AdrenalineImplantComponent"/>, so its damage reaches the implant.
/// </summary>
[RegisterComponent]
public sealed partial class AdrenalineHostComponent : Component;
