// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Preferences;
using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.Silicons.StationAi;

/// <summary>
/// Character whose body the AI's hologram shows. Null means the regular customization sprite.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class AiHologramProfileComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public HumanoidCharacterProfile? Profile;

    /// <summary>
    /// Client-side entity the hologram copies its sprite from.
    /// </summary>
    [ViewVariables]
    public EntityUid? Doll;
}

[ByRefEvent]
public readonly record struct AiHologramCustomizationChangedEvent;
