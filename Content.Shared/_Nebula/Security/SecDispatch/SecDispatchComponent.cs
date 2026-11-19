// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.Security.SecDispatch;

/// <summary>
/// Mask with a built-in transmitter: grants an action that opens a radial menu of preset radio reports.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SecDispatchComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionSecDispatch";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField]
    public ProtoId<RadioChannelPrototype> Channel = "Security";

    /// <summary>
    /// Cooldown put on the action after a report is sent.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Reports in menu order: clockwise starting from 9 o'clock, so with eight entries the first four fill the top half.
    /// </summary>
    [DataField(required: true)]
    public List<SecDispatchEntry> Entries = [];
}
