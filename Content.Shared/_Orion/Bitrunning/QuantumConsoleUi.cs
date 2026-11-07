// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Shared/_Orion/Bitrunning/QuantumConsoleUi.cs

using System.Linq;
using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Bitrunning;

[Serializable, NetSerializable]
public enum QuantumConsoleUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class QuantumConsoleBoundUiState : BoundUserInterfaceState
{
    public bool Connected;
    public NetEntity? Server;
    public string? CurrentDomain;
    public int Occupants;
    public int ConnectedPods;
    public int ServerPoints;
    public int ScannerTier;
    public BitrunningServerState State;
    public bool Broadcast;
    public bool ExtremeDifficultyUnlocked;
    public float CooldownTotalSeconds;
    public TimeSpan CooldownEndTime; // Nebula edit
    public List<BitrunningDomainListing> Domains;
    public List<BitrunningOccupantListing> ConnectedAvatars;

    public QuantumConsoleBoundUiState(
        bool connected,
        NetEntity? server,
        string? currentDomain,
        int occupants,
        int connectedPods,
        int serverPoints,
        int scannerTier,
        BitrunningServerState state,
        bool broadcast,
        bool extremeDifficultyUnlocked,
        float cooldownTotalSeconds,
        TimeSpan cooldownEndTime, // Nebula edit
        List<BitrunningDomainListing> domains,
        List<BitrunningOccupantListing> connectedAvatars)
    {
        Connected = connected;
        Server = server;
        CurrentDomain = currentDomain;
        Occupants = occupants;
        ConnectedPods = connectedPods;
        ServerPoints = serverPoints;
        ScannerTier = scannerTier;
        State = state;
        Broadcast = broadcast;
        ExtremeDifficultyUnlocked = extremeDifficultyUnlocked;
        CooldownTotalSeconds = cooldownTotalSeconds;
        CooldownEndTime = cooldownEndTime; // Nebula edit
        Domains = domains;
        ConnectedAvatars = connectedAvatars;
    }

    // Nebula start
    /// <summary>
    /// Value equality, so the periodic refresh does not resend an unchanged state.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is QuantumConsoleBoundUiState other
            && Connected == other.Connected
            && Server == other.Server
            && CurrentDomain == other.CurrentDomain
            && Occupants == other.Occupants
            && ConnectedPods == other.ConnectedPods
            && ServerPoints == other.ServerPoints
            && ScannerTier == other.ScannerTier
            && State == other.State
            && Broadcast == other.Broadcast
            && ExtremeDifficultyUnlocked == other.ExtremeDifficultyUnlocked
            && CooldownTotalSeconds.Equals(other.CooldownTotalSeconds)
            && CooldownEndTime == other.CooldownEndTime
            && Domains.SequenceEqual(other.Domains)
            && ConnectedAvatars.SequenceEqual(other.ConnectedAvatars);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Server, CurrentDomain, State, ServerPoints, CooldownEndTime);
    }
    // Nebula end
}

[Serializable, NetSerializable]
public sealed record BitrunningDomainListing // Nebula edit
{
    public string Id;
    public string Name;
    public string Description;
    public int Cost;
    public string Reward;
    public BitrunningDifficulty Difficulty;
    public bool IsModular;
    public bool HasSecondaryObjectives;

    public BitrunningDomainListing(string id, string name, string description, int cost, string reward, BitrunningDifficulty difficulty, bool isModular, bool hasSecondaryObjectives)
    {
        Id = id;
        Name = name;
        Description = description;
        Cost = cost;
        Reward = reward;
        Difficulty = difficulty;
        IsModular = isModular;
        HasSecondaryObjectives = hasSecondaryObjectives;
    }
}

[Serializable, NetSerializable]
public sealed record BitrunningOccupantListing // Nebula edit
{
    public string Name;
    public bool NoHit;

    public BitrunningOccupantListing(string name, bool noHit)
    {
        Name = name;
        NoHit = noHit;
    }
}

[Serializable, NetSerializable]
public sealed class QuantumConsoleLoadDomainMessage(string domainId) : BoundUserInterfaceMessage
{
    public string DomainId = domainId;
}

[Serializable, NetSerializable]
public sealed class QuantumConsoleRandomDomainMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class QuantumConsoleStopDomainMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class QuantumConsoleRefreshMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class QuantumConsoleBroadcastMessage(bool enabled) : BoundUserInterfaceMessage
{
    public bool Enabled = enabled;
}
