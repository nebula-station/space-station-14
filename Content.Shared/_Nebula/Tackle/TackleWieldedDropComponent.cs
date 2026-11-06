// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.Tackle;

/// <summary>
/// Gloves whose disarming tackle drops only the items the target wields with both hands.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TackleWieldedDropComponent : Component
{
    /// <summary>
    /// Lets gloves inheriting from security gloves keep dropping everything.
    /// </summary>
    [DataField(readOnly: true)]
    public bool Enabled = true;
}
