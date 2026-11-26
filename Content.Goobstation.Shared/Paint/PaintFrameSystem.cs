// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Goobstation.Shared/Paint/PaintFrameSystem.cs

using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Goobstation.Shared.Paint;

public sealed partial class PaintFrameSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<PaintCanvasVisualsComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !HasComp<PaintingFrameComponent>(args.Used))
            return;

        args.Handled = TryFrame(ent.Owner, args.Used, args.User);
    }

    public bool TryFrame(Entity<PaintCanvasVisualsComponent?> painting, EntityUid frameUid, EntityUid user)
    {
        if (!Resolve(painting, ref painting.Comp, false) || !TryComp<PaintingFrameComponent>(frameUid, out var frame))
            return false;

        if (!TryComp<PaintSurfaceComponent>(painting, out var surface) || !surface.Finished)
        {
            _popup.PopupEntity(Loc.GetString("paint-frame-unfinished"), painting, user);
            return true;
        }

        if (painting.Comp.Frame != null)
        {
            _popup.PopupEntity(Loc.GetString("paint-frame-already-framed"), painting, user);
            return true;
        }

        painting.Comp.Frame = frame.Material;
        Dirty(painting);
        _popup.PopupEntity(Loc.GetString("paint-frame-applied", ("painting", painting.Owner)), painting, user);
        QueueDel(frameUid);
        return true;
    }
}
