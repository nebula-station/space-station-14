// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client._Nebula.Lobby;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Trauma.Common.Inventory;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed partial class ProfilePreviewSpriteView
{
    private static readonly ProtoId<InventorySlotPrototype>[] UnderwearSlots = { "underwearb", "underweart", "socks" };

    /// <summary>
    /// Reloads the dummy dressed either in the job clothing or in the wardrobe items, in their underwear only or naked.
    /// </summary>
    public void LoadPreview(HumanoidCharacterProfile profile, JobPrototype? jobOverride, bool wardrobe, NebulaPreviewClothing clothing)
    {
        var dressed = clothing != NebulaPreviewClothing.Naked;
        LoadPreview(profile, jobOverride, dressed && !wardrobe);

        if (dressed && wardrobe)
            GiveDummyWardrobe(profile);

        if (clothing == NebulaPreviewClothing.Underwear)
            StripToUnderwear();
    }

    /// <summary>
    /// Takes off everything except the underpants, the bra and the socks. Goes from the last slot, so pockets and belts come off
    /// before the jumpsuit they hang on.
    /// </summary>
    private void StripToUnderwear()
    {
        var inventory = EntMan.System<InventorySystem>();
        if (!inventory.TryGetSlots(PreviewDummy, out var slots))
            return;

        for (var i = slots.Length - 1; i >= 0; i--)
        {
            var slot = slots[i].Name;
            if (Array.IndexOf(UnderwearSlots, slot) >= 0)
                continue;

            if (inventory.TryUnequip(PreviewDummy, slot, out var item, silent: true, force: true, reparent: false))
                EntMan.DeleteEntity(item.Value);
        }
    }

    /// <summary>
    /// Puts every selected wardrobe item into the first free slot that fits it.
    /// </summary>
    private void GiveDummyWardrobe(HumanoidCharacterProfile profile)
    {
        var inventory = EntMan.System<InventorySystem>();
        if (!inventory.TryGetSlots(PreviewDummy, out var slots))
            return;

        foreach (var id in profile.Wardrobe)
        {
            if (!_prototypeManager.HasIndex(id))
                continue;

            var item = EntMan.SpawnEntity(id, MapCoordinates.Nullspace);
            if (!EntMan.TryGetComponent<ClothingComponent>(item, out var clothing)
                || !slots.Any(slot => (slot.SlotFlags & clothing.Slots) != 0
                    && inventory.TryEquip(PreviewDummy, item, slot.Name, silent: true, force: true)))
            {
                EntMan.DeleteEntity(item);
            }
        }
    }
}
