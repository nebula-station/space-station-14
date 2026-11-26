// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/02b6d617ada28d2a78cfd227b9860838fb082909/Content.Shared/Research/Components/ResearchServerControlConsoleComponent.Arcane.cs

using Robust.Shared.Serialization;

namespace Content.Shared.Research.Components;

[RegisterComponent]
public sealed partial class ResearchServerControlConsoleComponent : Component;

[RegisterComponent]
public sealed partial class ResearchServerControlStatusComponent : Component
{
    [DataField]
    public bool GenerationEnabled = true;
}

[NetSerializable, Serializable]
public enum ResearchServerControlUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed partial class ToggleServerGenerationMessage : BoundUserInterfaceMessage
{
    public int ServerId;

    public ToggleServerGenerationMessage(int serverId)
    {
        ServerId = serverId;
    }
}

[Serializable, NetSerializable]
public sealed partial class ResearchServerControlBoundInterfaceState : BoundUserInterfaceState
{
    public List<ResearchServerControlEntry> Servers;

    public ResearchServerControlBoundInterfaceState(List<ResearchServerControlEntry> servers)
    {
        Servers = servers;
    }
}

[Serializable, NetSerializable]
public sealed class ResearchServerControlEntry
{
    public int Id;
    public NetEntity ServerEntity;
    public string Name;
    public string NetworkId;
    public bool IsNetworkAuthority;
    public int NetworkAuthorityId;
    public bool GenerationEnabled;
    public int TotalPointsPerSecond;
    public List<ResearchPointAmount> PointGeneration;
    public List<ResearchPointAmount> Balances;
    public int LogCount;

    public ResearchServerControlEntry(int id, NetEntity serverEntity, string name, string networkId, bool isNetworkAuthority, int networkAuthorityId, bool generationEnabled, int totalPointsPerSecond, List<ResearchPointAmount> pointGeneration, List<ResearchPointAmount> balances, int logCount)
    {
        Id = id;
        ServerEntity = serverEntity;
        Name = name;
        NetworkId = networkId;
        IsNetworkAuthority = isNetworkAuthority;
        NetworkAuthorityId = networkAuthorityId;
        GenerationEnabled = generationEnabled;
        TotalPointsPerSecond = totalPointsPerSecond;
        PointGeneration = pointGeneration;
        Balances = balances;
        LogCount = logCount;
    }
}
