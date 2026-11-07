// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Shared/_Orion/Bitrunning/Components/BitrunningPointsVendorComponent.cs

using Robust.Shared.GameStates;

namespace Content.Shared._Orion.Bitrunning.Components;

/// <summary>
/// Makes a shop vendor use bitrunning points to buy items.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BitrunningPointsVendorComponent : Component;
