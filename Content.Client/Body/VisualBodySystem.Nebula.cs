// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Humanoid.Markings;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Containers;

namespace Content.Client.Body;

/// <summary>
/// Nebula - WolfGate. Draws the size step and skin-toned art of markings with <see cref="MarkingPrototype.SizedStates"/>.
/// </summary>
public sealed partial class VisualBodySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedContainerSystem _containers = default!;

    /// <summary>
    /// Slots that cover the chest and groin, the same ones the ERP panel checks for exposed body parts.
    /// </summary>
    private const SlotFlags GenitalCoveringSlots = SlotFlags.INNERCLOTHING | SlotFlags.OUTERCLOTHING;

    private void InitializeGenitals()
    {
        SubscribeLocalEvent<HumanoidProfileComponent, DidEquipEvent>(OnGenitalsEquip);
        SubscribeLocalEvent<HumanoidProfileComponent, DidUnequipEvent>(OnGenitalsUnequip);
    }

    private void OnGenitalsEquip(EntityUid uid, HumanoidProfileComponent comp, DidEquipEvent args)
    {
        if ((args.SlotFlags & GenitalCoveringSlots) != 0)
            RefreshGenitals(uid);
    }

    private void OnGenitalsUnequip(EntityUid uid, HumanoidProfileComponent comp, DidUnequipEvent args)
    {
        if ((args.SlotFlags & GenitalCoveringSlots) != 0)
            RefreshGenitals(uid);
    }

    /// <summary>
    /// Redraws the markings of organs that can carry genitals, so they hide or show with the clothing.
    /// </summary>
    private void RefreshGenitals(EntityUid body)
    {
        if (!_containers.TryGetContainer(body, BodyComponent.ContainerID, out var organs))
            return;

        foreach (var organ in organs.ContainedEntities)
        {
            if (!TryComp<VisualOrganMarkingsComponent>(organ, out var markings)
                || !markings.MarkingData.Layers.Contains(HumanoidVisualLayers.Breasts))
                continue;

            RemoveMarkings((organ, markings), body);
            ApplyMarkings((organ, markings), body);
        }
    }

    /// <summary>
    /// Jumpsuits and outer clothing cover the genitals; undergarment markings don't, like on the ERP panel.
    /// </summary>
    private bool GenitalsCovered(Entity<VisualOrganMarkingsComponent> organ)
    {
        if (Comp<OrganComponent>(organ).Body is not { } body
            || !_inventory.TryGetContainerSlotEnumerator(body, out var slots, GenitalCoveringSlots))
            return false;

        return slots.NextItem(out _);
    }

    private void ApplySizedState(Entity<VisualOrganMarkingsComponent> organ,
        Entity<SpriteComponent> target,
        int spriteLayer,
        MarkingPrototype proto,
        Marking marking,
        int spriteIndex)
    {
        if (proto.SizedStates is not { } sized
            || !_sprite.TryGetLayer(target.AsNullable(), spriteLayer, out var layer, false)
            || layer.ActualRsi is not { } rsi)
            return;

        var tone = ProtoMan.Resolve(organ.Comp.MarkingData.Group, out var group) ? group.ArtTone : string.Empty;

        // toned art first, then the neutral art for shapes that have no toned variant
        if (TrySetState(target, spriteLayer, rsi, sized.State(spriteIndex, marking.Size, tone))
            || tone.Length > 0 && TrySetState(target, spriteLayer, rsi, sized.State(spriteIndex, marking.Size, string.Empty)))
            return;

        // no art for this step, e.g. small breasts have nothing behind the body
        _sprite.LayerSetRsiState(target.AsNullable(), spriteLayer, RSI.StateId.Invalid);
    }

    private bool TrySetState(Entity<SpriteComponent> target, int spriteLayer, RSI rsi, string? state)
    {
        if (state == null || !rsi.TryGetState(state, out _))
            return false;

        _sprite.LayerSetRsiState(target.AsNullable(), spriteLayer, state);
        return true;
    }
}
