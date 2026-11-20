// SPDX-License-Identifier: AGPL-3.0-or-later

// Ported from https://github.com/Goob-Station/Goob-Station/blob/707e36693d1d9289a4f04921c5e070b1e4195589/Content.Shared/_ES/Camera/Trigger/ESScreenshakeUserOnTriggerComponent.cs

using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Camera.Trigger;

/// <summary>
/// Screenshakes the user on trigger.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESScreenshakeUserOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField]
    public ESScreenshakeParameters? Translation;

    [DataField]
    public ESScreenshakeParameters? Rotation;
}
