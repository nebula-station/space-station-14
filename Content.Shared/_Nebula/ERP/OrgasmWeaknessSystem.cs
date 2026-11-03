// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Movement.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Nebula.ERP;

public sealed partial class OrgasmWeaknessSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OrgasmWeaknessComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<OrgasmWeaknessComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<OrgasmWeaknessComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_net.IsServer)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<OrgasmWeaknessComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now >= comp.ExpiresAt)
                RemCompDeferred<OrgasmWeaknessComponent>(uid);
        }
    }

    private void OnInit(Entity<OrgasmWeaknessComponent> ent, ref ComponentInit args)
    {
        _speed.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void OnRefreshSpeed(Entity<OrgasmWeaknessComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.SpeedModifier, ent.Comp.SpeedModifier);
    }

    private void OnShutdown(Entity<OrgasmWeaknessComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.SpeedModifier = 1f;
        _speed.RefreshMovementSpeedModifiers(ent.Owner);
    }
}
