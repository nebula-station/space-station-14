// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared.Emag.Systems;

namespace Content.Shared._Nebula.Bitrunning;

/// <summary>
/// Lets an emag unlock extreme domains on a quantum server once.
/// </summary>
public sealed partial class QuantumServerEmagSystem : EntitySystem
{
    [Dependency] private EmagSystem _emag = default!;

    [SubscribeLocalEvent]
    private void OnEmagged(Entity<QuantumServerComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction) || _emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;
    }
}
