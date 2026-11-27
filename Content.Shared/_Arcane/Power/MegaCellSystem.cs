// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Shared/Power/MegaCellSystem.cs

using Content.Shared.PowerCell.Components;
using Robust.Shared.Containers;

namespace Content.Shared._Arcane.Power;

public sealed partial class MegaCellSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnInsertAttempt(EntityUid uid, MegaCellComponent comp, ContainerGettingInsertedAttemptEvent args)
    {
        var owner = args.Container.Owner;
        if (TryComp<PowerCellSlotComponent>(owner, out var slot)
            && slot.CellSlotId == args.Container.ID
            && !HasComp<MegaCellSlotComponent>(owner))
            args.Cancel();
    }
}
