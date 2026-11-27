// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/2e7ace0bdb6965d914d8417c8b16ca25b6760918/Content.Arcane.Shared/ToggleableHUD/ToggleableHUDComponent.cs

using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Arcane.ToggleableHUD;

/// <summary>
/// Gives the entity an action that toggles every station HUD overlay at once (Orion #246).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ToggleableHUDComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionToggleHUDs";

    /// <summary>
    /// The granted action, set at runtime.
    /// </summary>
    [DataField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Whether the overlays are on. Only the action changes it: true in YAML adds no overlays by itself.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsToggled;

    /// <summary>
    /// Damage containers whose health bars the toggled HUD shows.
    /// </summary>
    [DataField]
    public List<ProtoId<DamageContainerPrototype>> DamageContainers =
    [
        "Biological",
        "Inorganic",
        "Silicon",
        "SiliconRadiation",
    ];

    [DataField]
    public LocId PopupToggleOn = "ghost-gui-toggle-hud-popup-on";

    [DataField]
    public LocId PopupToggleOff = "ghost-gui-toggle-hud-popup-off";
}
