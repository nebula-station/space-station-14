// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;

namespace Content.Shared._Nebula.InfinityDorms.Components;

[RegisterComponent]
public sealed partial class InfinityDormComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid Creator;

    [ViewVariables(VVAccess.ReadOnly)]
    public int Number;

    [ViewVariables]
    public bool IsClosed = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityCoordinates Coordinates;

    [ViewVariables(VVAccess.ReadOnly)]
    public MapCoordinates MapCoordinates;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid TeleporterUid;

    [DataField]
    public string MapName = string.Empty;
}
