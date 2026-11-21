// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Dash;
using Content.Shared._LuaM.MantisBlades;

namespace Content.Goobstation.Shared._Nebula.MantisBlades;

/// <summary>
/// Performs the mantis blade combo dash. The action is given without <see cref="DashActionComponent"/>,
/// so its event is handled here, in shared code to be predicted.
/// </summary>
public sealed partial class MantisBladeDashSystem : EntitySystem
{
    [Dependency] private DashActionSystem _dash = default!;

    [SubscribeLocalEvent]
    private void OnDash(Entity<MantisBladeComboUserComponent> ent, ref DashActionEvent args)
    {
        _dash.TryDash(ent, args);
    }
}
