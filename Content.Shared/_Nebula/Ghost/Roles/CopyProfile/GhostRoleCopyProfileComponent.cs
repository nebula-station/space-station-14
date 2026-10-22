// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.Ghost.Roles.CopyProfile;

[RegisterComponent]
public sealed partial class GhostRoleCopyProfileComponent : Component
{
    [DataField]
    public ProtoId<StartingGearPrototype>? Gear;

    public HashSet<(NetUserId User, int Slot)> Used = new();
}


[RegisterComponent]
public sealed partial class PendingProfileCopyComponent : Component
{
    public EntityUid Spawner;
    public int Slot;
    public ProtoId<StartingGearPrototype>? Gear;
}
