// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Shared/_Orion/Bitrunning/Components/BitrunningPointsComponent.cs

using Content.Shared._Orion.Bitrunning.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Orion.Bitrunning.Components;

/// <summary>
/// Stores bitrunning points for a holder, such as an ID card.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(BitrunningPointsSystem))]
[AutoGenerateComponentState]
public sealed partial class BitrunningPointsComponent : Component
{
    /// <summary>
    /// The number of stored bitrunning points.
    /// </summary>
    [DataField, AutoNetworkedField]
    public uint Points;
}
