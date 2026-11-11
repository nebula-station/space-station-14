// SPDX-License-Identifier: AGPL-3.0-only
// Ported from https://github.com/AdventureTimeSS14/space_station_ADT/blob/296c8b93290412fb8b446c0f42ce107851eefedf/Content.Shared/ADT/Posing/PosingComponent.cs

using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._ADT.Posing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class PosingComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Vector2 CurrentOffset = Vector2.Zero;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Angle CurrentAngle = Angle.Zero;

    [DataField, AutoNetworkedField]
    public Vector2 OffsetLimits = new(0.3f, 0.3f);

    [DataField, AutoNetworkedField]
    public float AngleLimits = 180f;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Posing = false;

    [DataField]
    public string DefaultInputContext = "human";

    [DataField]
    public Vector2 DefaultOffset = Vector2.Zero;

    [DataField]
    public float DefaultAngle = 0f;
}
