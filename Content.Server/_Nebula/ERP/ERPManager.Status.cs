// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.ERP;
using Content.Shared.Humanoid;
using Robust.Shared.Player;

namespace Content.Server._Nebula.ERP;

public sealed partial class ERPManager
{
    private void OnPlayerAttached(Entity<ERPManagerComponent> ent, ref PlayerAttachedEvent args)
    {
        if (!TryComp<HumanoidProfileComponent>(ent.Owner, out var profile))
            return;

        if (ent.Comp.Status != profile.ERPStatus)
        {
            ent.Comp.Status = profile.ERPStatus;
            Dirty(ent);
        }
    }
}
