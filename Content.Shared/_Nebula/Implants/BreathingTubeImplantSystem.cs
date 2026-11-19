// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;

namespace Content.Shared._Nebula.Implants;

/// <summary>
/// Connects an enabled <see cref="BreathingTubeImplantComponent"/> to the internals of its body.
/// </summary>
public sealed partial class BreathingTubeImplantSystem : EntitySystem
{
    [Dependency] private SharedInternalsSystem _internals = default!;

    [SubscribeLocalEvent]
    private void OnEnabled(Entity<BreathingTubeImplantComponent> ent, ref OrganEnabledEvent args)
    {
        if (TryComp<InternalsComponent>(args.Body, out var internals))
            _internals.ConnectBreathTool((args.Body, internals), ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnDisabled(Entity<BreathingTubeImplantComponent> ent, ref OrganDisabledEvent args)
    {
        if (TryComp<InternalsComponent>(args.Body, out var internals))
            _internals.DisconnectBreathTool((args.Body, internals), ent.Owner, forced: true);
    }
}
