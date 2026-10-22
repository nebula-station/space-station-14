// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Nebula.Silicons.SwitchableState;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BorgSwitchableStateComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<BorgStateType, Vector2> StatesWhiteList = [];

    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier BaseSprite;

    [DataField, AutoNetworkedField]
    public BorgStateType CurrentType = BorgStateType.Base;

    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "SwitchState";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField, AutoNetworkedField]
    public TimeSpan DoAfterDuration = TimeSpan.FromSeconds(3);

    public bool IsRestActive = false;
}
