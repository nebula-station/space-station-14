// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/accb2613b4ea35df58380cc8c7c82cfb2968670a/Content.Arcane.Shared/Economy/CreditHolochipComponent.cs

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Arcane.Economy;

/// <summary>
///     Credit stack shown as a holochip, coloured by its amount. EMPs can wipe it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CreditHolochipComponent : Component;

[Serializable, NetSerializable]
public enum CreditHolochipVisuals : byte
{
    BaseState,
    OverlayState,
    BaseColor,
}
