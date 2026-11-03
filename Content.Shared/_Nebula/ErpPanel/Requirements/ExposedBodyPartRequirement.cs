// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.ErpPanel.Requirements;

[Serializable, NetSerializable]
public enum ErpBodyZone : byte
{
    Chest,
    Groin,
}

[Serializable, NetSerializable]
public sealed partial class ExposedBodyPartRequirement : InvertableErpRequirement
{
    [DataField(required: true)]
    public ErpBodyZone Part;

    public override bool IsAvailable(EntityUid uid, IEntityManager entityManager)
    {
        // there are no underwear slots, and undergarment markings can't be taken off, so only clothes count
        var coveringSlots = Part switch
        {
            ErpBodyZone.Chest => SlotFlags.INNERCLOTHING | SlotFlags.OUTERCLOTHING,
            ErpBodyZone.Groin => SlotFlags.INNERCLOTHING | SlotFlags.OUTERCLOTHING,
            _ => SlotFlags.NONE,
        };

        if (coveringSlots == SlotFlags.NONE)
            return false;

        var inventory = entityManager.System<InventorySystem>();
        if (!inventory.TryGetContainerSlotEnumerator(uid, out var slots, coveringSlots))
            return false;

        var isExposed = !slots.NextItem(out _);

        return Inverted ? !isExposed : isExposed;
    }
}
