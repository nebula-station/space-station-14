// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Ghost.Roles.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Ghost.Roles;

public sealed partial class GhostRoleSystem
{
    /// <summary>
    /// Role types of the mind roles the ghost role gives, or of its mind's roles when it already has a mind.
    /// </summary>
    private List<ProtoId<RoleTypePrototype>> GetRoleTypes(Entity<GhostRoleComponent> roleEnt)
    {
        var types = new List<ProtoId<RoleTypePrototype>>();

        if (TryComp<MindContainerComponent>(roleEnt, out var mindCont)
            && TryComp<MindComponent>(mindCont.Mind, out var mind))
        {
            foreach (var role in mind.MindRoleContainer.ContainedEntities)
            {
                if (TryComp<MindRoleComponent>(role, out var comp) && comp.RoleType is { } type)
                    types.Add(type);
            }

            return types;
        }

        foreach (var proto in roleEnt.Comp.MindRoles)
        {
            if (ProtoMan.TryIndex(proto, out var indexed)
                && indexed.TryComp<MindRoleComponent>(out var comp, Factory)
                && comp.RoleType is { } type)
            {
                types.Add(type);
            }
        }

        return types;
    }
}
