// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/02b6d617ada28d2a78cfd227b9860838fb082909/Content.Shared/Research/ResearchMachineVisuals.Arcane.cs

using Robust.Shared.Serialization;

namespace Content.Shared.Research;

[Serializable, NetSerializable]
public enum DestructiveAnalyzerVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum DestructiveAnalyzerVisualState : byte
{
    Idle,
    Inserting,
    Loaded,
    Deconstructing,
}

[Serializable, NetSerializable]
public enum ExperimentalDestructiveScannerVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum ExperimentalDestructiveScannerVisualState : byte
{
    Idle,
    Up,
    Down,
    Scanning,
}


[Serializable, NetSerializable]
public enum DestructiveAnalyzerVisualLayers : byte
{
    Base,
}

[Serializable, NetSerializable]
public enum ExperimentalDestructiveScannerVisualLayers : byte
{
    Base,
}
