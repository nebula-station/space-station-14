// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Server._Nebula.PointTether;
using Content.Shared._Nebula.PointTether;
using Content.Shared.Holopad;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Prototypes;

namespace Content.Server._Nebula.Silicons.StationAi;

public sealed partial class AiHologramWalkSystem : EntitySystem
{
    [Dependency] private PointTetherSystem _tether = default!;
    [Dependency] private SharedStationAiSystem _stationAi = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly EntProtoId WalkingEye = "StationAiHoloWalking";
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(0.2);
    private const float DissolveRange = 6f;

    /// <summary>
    /// Switches the AI to the holopad view with an eye that can walk and carry the holopad's hologram.
    /// </summary>
    public void BeginWalk(Entity<StationAiCoreComponent?> core, Entity<HolopadComponent> holopad)
    {
        if (!Resolve(core, ref core.Comp))
            return;

        // SetupEye reads the prototype from here. The eye is spawned before the original value is restored.
        var physicalProto = core.Comp.PhysicalEntityProto;
        core.Comp.PhysicalEntityProto = WalkingEye;
        _stationAi.SwitchRemoteEntityMode(core, false);
        core.Comp.PhysicalEntityProto = physicalProto;

        if (core.Comp.RemoteEntity is not { } eye
            || Prototype(eye)?.ID != WalkingEye.Id
            || holopad.Comp.Hologram is not { } hologram)
            return;

        var walker = EnsureComp<AiHologramWalkerComponent>(eye);
        walker.Holopad = holopad.Owner;
        walker.Hologram = hologram;

        _transform.Unanchor(hologram);
        _transform.SetParent(hologram, eye);
        _transform.SetLocalPosition(hologram, Vector2.Zero);

        _tether.SetTether(eye, holopad.Owner, DissolveRange, CheckInterval);
    }

    [SubscribeLocalEvent]
    private void OnTetherExceeded(Entity<AiHologramWalkerComponent> ent, ref PointTetherExceededEvent args)
    {
        _transform.SetCoordinates(ent.Owner, Transform(ent.Comp.Holopad).Coordinates);
    }

    [SubscribeLocalEvent]
    private void OnWalkerTerminating(Entity<AiHologramWalkerComponent> ent, ref EntityTerminatingEvent args)
    {
        // The holopad still owns the hologram. Deleting it along with its parent would leave a stale reference.
        if (TerminatingOrDeleted(ent.Comp.Hologram) || TerminatingOrDeleted(ent.Comp.Holopad))
            return;

        _transform.SetCoordinates(ent.Comp.Hologram, Transform(ent.Comp.Holopad).Coordinates);
    }
}
