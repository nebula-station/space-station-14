// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Shared/_Orion/Bitrunning/BitrunningShared.cs

using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Bitrunning;

[NetSerializable, Serializable]
public enum BitrunningServerState : byte
{
    Ready,
    Running,
    CoolingDown,
}

[NetSerializable, Serializable]
public enum BitrunningDifficulty : byte
{
    Peaceful,
    Easy,
    Medium,
    Hard,
    Extreme, // Visible only when server emagged!!!
}

[NetSerializable, Serializable]
public enum BitrunningObjectiveType : byte
{
    None,
    CollectEncryptedCaches, // Collect all caches
    DeliveryCacheCrate, // Delivery cache crate to markers
    EliminateEnemies, // Kill all marked enemy
    CatchFish, // Catch fish
    FillStomach, // Overfeed avatar
    OverhydrateStomach, // Overhydrate avatar
}

[NetSerializable, Serializable]
public enum NetpodVisuals : byte
{
    State,
}

[NetSerializable, Serializable]
public enum NetpodVisualState : byte
{
    Open,
    Closed,
    Active,
    OpenActive,
    Opening,
    Closing,
}
