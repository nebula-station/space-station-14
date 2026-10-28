// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.Discord.Linking;

public enum DiscordLinkingCodeStatus : byte
{
    Ok,
    AlreadyLinked,
    Unavailable,
    Error,
}

public sealed class DiscordLinkingCodeMsg : NetMessage
{
    public DiscordLinkingCodeStatus Status;
    public Guid Code;

    public override MsgGroups MsgGroup => MsgGroups.Core;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Status = (DiscordLinkingCodeStatus) buffer.ReadByte();
        Code = buffer.ReadGuid();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write((byte) Status);
        buffer.Write(Code);
    }
}
