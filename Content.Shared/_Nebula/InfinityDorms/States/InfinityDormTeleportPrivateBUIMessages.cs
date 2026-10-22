// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.InfinityDorms.States;

[Serializable, NetSerializable]
public sealed class InfinityDormTeleportPrivateBUIMessage(List<InfinityDormRoomData>? rooms) : BoundUserInterfaceMessage
{
    public readonly List<InfinityDormRoomData>? Rooms = rooms;
}
