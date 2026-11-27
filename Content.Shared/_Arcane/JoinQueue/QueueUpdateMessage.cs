// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/dfbda555db9acabf3827cf3b0785c6deaa5c1a8b/Content.Arcane.Shared/JoinQueue/QueueUpdateMessage.cs

using System.IO;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Arcane.JoinQueue;

/// <summary>
///     Sent from server to client with queue state for player
///     Also initiates queue state on client
/// </summary>
public sealed class QueueUpdateMessage : NetMessage
{
    private const int MaxQueuePlayerEntries = 1000;
    private const int MaxMiniGameLeaderboardEntries = 1000;
    public override MsgGroups MsgGroup => MsgGroups.Command;

    // Queue info
    public int Total { get; set; }
    public int Position { get; set; }

    // Estimated wait
    public float EstimatedWaitSeconds { get; set; } = -1f;

    // Server info
    public string MapName { get; set; } = string.Empty;
    public string GameMode { get; set; } = string.Empty;
    public int ServerPlayerCount { get; set; }
    public int MaxPlayerCount { get; set; }
    public int RoundDurationMinutes { get; set; }

    // Critter display
    public string YourName { get; set; } = string.Empty;
    public int YourQueueWaitLeaderboardIndex { get; set; } = -1;
    public List<string> PlayerNames { get; set; } = [];
    public List<float> PlayerWaitSeconds { get; set; } = [];
    public List<string> QueueWaitLeaderboardNames { get; set; } = [];
    public List<float> QueueWaitLeaderboardSeconds { get; set; } = [];
    public List<QueueMiniGameLeaderboardEntry> MiniGameLeaderboard { get; set; } = [];
    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Total = buffer.ReadInt32();
        Position = buffer.ReadInt32();
        EstimatedWaitSeconds = buffer.ReadFloat();

        MapName = buffer.ReadString();
        GameMode = buffer.ReadString();
        ServerPlayerCount = buffer.ReadInt32();
        MaxPlayerCount = buffer.ReadInt32();
        RoundDurationMinutes = buffer.ReadInt32();

        YourName = buffer.ReadString();
        YourQueueWaitLeaderboardIndex = buffer.ReadInt32();
        var count = buffer.ReadInt32();
        if (count < 0 || count > MaxQueuePlayerEntries)
            throw new InvalidDataException("Queue player count out of range.");

        PlayerNames = new List<string>(count);
        PlayerWaitSeconds = new List<float>(count);
        for (var i = 0; i < count; i++)
        {
            PlayerNames.Add(buffer.ReadString());
            PlayerWaitSeconds.Add(buffer.ReadFloat());
        }
        var waitLeaderboardCount = buffer.ReadInt32();
        if (waitLeaderboardCount < 0 || waitLeaderboardCount > MaxQueuePlayerEntries)
            throw new InvalidDataException("Queue wait leaderboard count out of range.");

        QueueWaitLeaderboardNames = new List<string>(waitLeaderboardCount);
        QueueWaitLeaderboardSeconds = new List<float>(waitLeaderboardCount);
        for (var i = 0; i < waitLeaderboardCount; i++)
        {
            QueueWaitLeaderboardNames.Add(buffer.ReadString());
            QueueWaitLeaderboardSeconds.Add(buffer.ReadFloat());
        }

        var leaderboardCount = buffer.ReadInt32();
        if (leaderboardCount < 0 || leaderboardCount > MaxMiniGameLeaderboardEntries)
            throw new InvalidDataException("Queue mini-game leaderboard count out of range.");

        MiniGameLeaderboard = new List<QueueMiniGameLeaderboardEntry>(leaderboardCount);
        for (var i = 0; i < leaderboardCount; i++)
        {
            var game = buffer.ReadByte();
            if (!Enum.IsDefined(typeof(QueueMiniGameKind), game))
                throw new InvalidDataException("Queue mini-game kind out of range.");

            MiniGameLeaderboard.Add(new QueueMiniGameLeaderboardEntry(
                (QueueMiniGameKind) game,
                buffer.ReadString(),
                buffer.ReadInt32()));
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Total);
        buffer.Write(Position);
        buffer.Write(EstimatedWaitSeconds);

        buffer.Write(MapName);
        buffer.Write(GameMode);
        buffer.Write(ServerPlayerCount);
        buffer.Write(MaxPlayerCount);
        buffer.Write(RoundDurationMinutes);

        buffer.Write(YourName);
        buffer.Write(YourQueueWaitLeaderboardIndex);
        var playerCount = Math.Min(Math.Min(PlayerNames.Count, PlayerWaitSeconds.Count), MaxQueuePlayerEntries);
        buffer.Write(playerCount);
        for (var i = 0; i < playerCount; i++)
        {
            buffer.Write(PlayerNames[i]);
            buffer.Write(PlayerWaitSeconds[i]);
        }

        var waitLeaderboardCount = Math.Min(Math.Min(QueueWaitLeaderboardNames.Count, QueueWaitLeaderboardSeconds.Count), MaxQueuePlayerEntries);
        buffer.Write(waitLeaderboardCount);
        for (var i = 0; i < waitLeaderboardCount; i++)
        {
            buffer.Write(QueueWaitLeaderboardNames[i]);
            buffer.Write(QueueWaitLeaderboardSeconds[i]);
        }

        var miniGameLeaderboardCount = Math.Min(MiniGameLeaderboard.Count, MaxMiniGameLeaderboardEntries);
        buffer.Write(miniGameLeaderboardCount);
        for (var i = 0; i < miniGameLeaderboardCount; i++)
        {
            var entry = MiniGameLeaderboard[i];
            buffer.Write((byte) entry.Game);
            buffer.Write(entry.PlayerName);
            buffer.Write(entry.Score);
        }
    }
}
