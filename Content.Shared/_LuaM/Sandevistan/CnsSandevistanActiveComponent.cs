// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Server/_Goobstation/Sandevistan/SandevistanSystem.cs

using Robust.Shared.GameStates;

namespace Content.Shared._LuaM.Sandevistan;

/// <summary>
/// Added while a CNS sandevistan is running. Shared so the client predicts the faster movement and attacks.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class CnsSandevistanActiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MovementSpeedModifier = 2f;

    [DataField, AutoNetworkedField]
    public float AttackSpeedModifier = 2f;
}
