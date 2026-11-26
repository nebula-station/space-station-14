// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Server/NPC/Systems/NPCUseActionOnTargetSystem.cs

using Content.Server.NPC.Components;
using Robust.Shared.Random;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCUseActionOnTargetSystem
{
    [Dependency] private IRobustRandom _random = default!;

    private void AddExtraActions(Entity<NPCUseActionOnTargetComponent> ent)
    {
        foreach (var actionId in ent.Comp.ExtraActionIds)
        {
            if (_actions.AddAction(ent, actionId) is { } extra)
                ent.Comp.ExtraActionEnts.Add(extra);
        }
    }

    /// <summary>
    /// Picks one of the main and extra actions.
    /// </summary>
    private EntityUid? PickAction(NPCUseActionOnTargetComponent comp)
    {
        if (comp.ExtraActionEnts.Count == 0)
            return comp.ActionEnt;

        var index = _random.Next(comp.ExtraActionEnts.Count + 1);
        return index > 0 ? comp.ExtraActionEnts[index - 1] : comp.ActionEnt;
    }
}
