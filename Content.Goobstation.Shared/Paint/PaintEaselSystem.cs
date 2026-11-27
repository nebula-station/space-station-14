// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Goobstation.Shared/Paint/PaintEaselSystem.cs

using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Robust.Shared.Containers;

namespace Content.Goobstation.Shared.Paint;

public sealed partial class PaintEaselSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private PaintFrameSystem _frame = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public EntityUid? GetCanvas(Entity<PaintEaselComponent> easel)
    {
        return _itemSlots.GetItemOrNull(easel.Owner, easel.Comp.Slot); // Nebula edit
    }

    public bool TryGetEasel(EntityUid canvas, out Entity<PaintEaselComponent> easel)
    {
        easel = default;
        if (!_container.TryGetContainingContainer((canvas, null, null), out var container)
            || !TryComp<PaintEaselComponent>(container.Owner, out var comp))
            return false;

        easel = (container.Owner, comp);
        return true;
    }

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<PaintEaselComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || GetCanvas(ent) is not { } canvas)
            return;

        if (HasComp<PaintingFrameComponent>(args.Used))
        {
            args.Handled = _frame.TryFrame(canvas, args.Used, args.User);
            return;
        }

        if (!HasComp<PaintToolComponent>(args.Used))
            return;

        args.Handled = TryOpen(canvas, args.User);
    }

    [SubscribeLocalEvent]
    private void OnActivate(Entity<PaintEaselComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex || GetCanvas(ent) is not { } canvas)
            return;

        args.Handled = TryOpen(canvas, args.User);
    }

    private bool TryOpen(EntityUid canvas, EntityUid user)
    {
        if (!_ui.HasUi(canvas, PaintUiKey.Key))
            return false;

        _ui.OpenUi(canvas, PaintUiKey.Key, user);
        return true;
    }

    [SubscribeLocalEvent]
    private void OnAccessibleOverride(Entity<PaintSurfaceComponent> ent, ref AccessibleOverrideEvent args)
    {
        if (args.Handled || args.Target != ent.Owner || !TryGetEasel(ent, out var easel))
            return;

        args.Handled = true;
        args.Accessible = _interaction.IsAccessible(args.User, easel.Owner);
    }
}
