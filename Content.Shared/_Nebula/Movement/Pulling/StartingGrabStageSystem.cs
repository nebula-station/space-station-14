// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;
using Content.Trauma.Common.MartialArts;

namespace Content.Shared._Nebula.Movement.Pulling;

public sealed partial class StartingGrabStageSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnGetStartingGrabStage(Entity<StartingGrabStageComponent> ent, ref InventoryRelayedEvent<GetStartingGrabStageEvent> args)
    {
        if (args.Args.Stage == GrabStage.Soft)
            args.Args.Stage = ent.Comp.Stage;
    }
}

/// <summary>
/// Raised on the puller to let worn gloves replace the stage a new grab would get.
/// </summary>
[ByRefEvent]
public record struct GetStartingGrabStageEvent(GrabStage Stage) : IInventoryRelayEvent
{
    public readonly SlotFlags TargetSlots => SlotFlags.GLOVES;
}
