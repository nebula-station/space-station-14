// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusIcon;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.InfinityDorms;

[Serializable, NetSerializable]
public enum InfinityDormTeleporterUiKey
{
    Key
}

[Serializable, NetSerializable]
public enum InfinityDormExitUiKey
{
    Key
}

[Serializable, NetSerializable]
public struct InfinityDormVisitorData
{
    public required ProtoId<JobIconPrototype> JobIconProtoId;
    public required string UserName;
}

public struct InfinityDormServerVisitorData
{
    public required InfinityDormVisitorData VisitorData;
    public required EntityUid UserId;
}

[Serializable, NetSerializable]
public struct InfinityDormRoomData
{
    public int Visitors;
    public TimeSpan DeletesAt;
    public string RoomName;
    public int RoomNumber;
}

