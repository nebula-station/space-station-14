// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/02b6d617ada28d2a78cfd227b9860838fb082909/Content.Shared/Research/ResearchDiscovery.Arcane.cs

using Robust.Shared.Serialization;

namespace Content.Shared.Research;

[Serializable, NetSerializable]
public enum ResearchDiscoveryEventType : byte
{
    ScanEntity,
    MachineInsertion,
    DeconstructEntity,
    ServerTrigger,
}

[Serializable, NetSerializable]
public enum ResearchTechnologyVisibilityState : byte
{
    Hidden,
    RevealedLocked,
    Available,
    Researched,
}
