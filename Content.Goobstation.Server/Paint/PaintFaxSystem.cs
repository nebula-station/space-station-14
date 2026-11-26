// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Goobstation.Server/Paint/PaintFaxSystem.cs

using Content.Goobstation.Shared.Fax;
using Content.Goobstation.Shared.Paint;
using Content.Shared.Popups;

namespace Content.Goobstation.Server.Paint;

public sealed partial class PaintFaxSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnFaxCopied(Entity<PaintSurfaceComponent> ent, ref GettingFaxedCopiedEvent args)
    {
        args.Handled = true;

        if (args.Args.Actor.IsValid())
            _popup.PopupEntity(Loc.GetString("paint-fax-use-photocopier"), args.Fax, args.Args.Actor);
    }

    [SubscribeLocalEvent]
    private void OnFaxSent(Entity<PaintSurfaceComponent> ent, ref GettingFaxedSentEvent args)
    {
        args.Handled = true;

        if (args.Actor.IsValid()) // Nebula edit
            _popup.PopupEntity(Loc.GetString("paint-fax-use-photocopier"), args.Fax, args.Actor); // Nebula edit
    }
}
