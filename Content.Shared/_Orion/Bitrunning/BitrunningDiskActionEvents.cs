// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Shared/_Orion/Bitrunning/BitrunningDiskActionEvents.cs

using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.Bitrunning;

public sealed partial class BitrunningSpawnCheeseActionEvent : InstantActionEvent
{
    [DataField]
    public EntProtoId PrototypeId = "FoodCheese";

    [DataField]
    public int Radius = 1;
}

public sealed partial class BitrunningLesserHealActionEvent : InstantActionEvent;
