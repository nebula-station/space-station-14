// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/02b6d617ada28d2a78cfd227b9860838fb082909/Content.Shared/Research/ResearchNetworkModels.Arcane.cs

using Robust.Shared.Serialization;

namespace Content.Shared.Research;

[DataDefinition, Serializable, NetSerializable]
public partial record struct ResearchPointAmount
{
    [DataField(required: true)]
    public string Type;

    [DataField]
    public int Amount;
}

[DataDefinition, Serializable, NetSerializable]
public partial record struct ResearchLogEntry
{
    [DataField]
    public TimeSpan Timestamp;

    [DataField(required: true)]
    public string Category;

    [DataField(required: true)]
    public string Message;

    [DataField]
    public NetEntity? Actor;
}
