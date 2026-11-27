// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;

namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

/// <summary>
/// Head implant that gives its owner a random face, name, DNA and fingerprints for a while.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class FacePanelImplantComponent : Component
{
    [DataField(readOnly: true)]
    public TimeSpan Duration = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Cooldown that starts once the disguise is off.
    /// </summary>
    [DataField(readOnly: true)]
    public TimeSpan Cooldown = TimeSpan.FromMinutes(3);

    /// <summary>
    /// When the disguise wears off, null while it is off.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan? DisguiseEnd;

    [ViewVariables]
    public string? OriginalDna;

    [ViewVariables]
    public string? OriginalFingerprint;
}

public sealed partial class FacePanelActionEvent : InstantActionEvent;
