// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Server/_Goobstation/Sandevistan/SandevistanSystem.cs

using Content.Goobstation.Shared.Sandevistan;

namespace Content.Server._LuaM.Cyberware;

/// <summary>
/// The psycho starts a heart sandevistan, but never turns it off.
/// </summary>
public sealed partial class CyberpsychosisSandevistanSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnPsychosisUse(Entity<SandevistanUserComponent> ent, ref CyberpsychosisUseImplantsEvent args)
    {
        if (ent.Comp.Active)
            return;

        var toggle = new ToggleSandevistanEvent { Performer = ent.Owner };
        RaiseLocalEvent(ent, toggle);
    }
}
