// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Client/Weapons/Ranged/TracerOverlay.cs

using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._Arcane.Weapons.Ranged;

public sealed class TracerOverlay : Overlay
{
    private readonly TracerSystem _tracer;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    public TracerOverlay(TracerSystem tracer)
    {
        _tracer = tracer;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        _tracer.Draw(args.WorldHandle, args.MapId);
    }
}
