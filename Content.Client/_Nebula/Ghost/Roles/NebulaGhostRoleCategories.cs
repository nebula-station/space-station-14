// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface;
using Content.Shared.Ghost.Roles;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._Nebula.Ghost.Roles;

/// <summary>
/// Section of the ghost roles window, in the order the sections are shown.
/// </summary>
public enum NebulaGhostRoleCategory : byte
{
    Antagonist,
    CentralCommand,
    FreeAgent,
    Silicon,
    Familiar,
    Other,
}

public static class NebulaGhostRoleCategories
{
    private static readonly ProtoId<DepartmentPrototype> CentralCommand = "CentralCommand";

    private static readonly Dictionary<ProtoId<RoleTypePrototype>, NebulaGhostRoleCategory> RoleTypes = new()
    {
        ["SoloAntagonist"] = NebulaGhostRoleCategory.Antagonist,
        ["TeamAntagonist"] = NebulaGhostRoleCategory.Antagonist,
        ["SiliconAntagonist"] = NebulaGhostRoleCategory.Antagonist,
        ["FreeAgent"] = NebulaGhostRoleCategory.FreeAgent,
        ["Silicon"] = NebulaGhostRoleCategory.Silicon,
        ["Familiar"] = NebulaGhostRoleCategory.Familiar,
        ["Animal"] = NebulaGhostRoleCategory.Familiar,
    };

    /// <summary>
    /// Antagonist role types win over anything else, then Central Command jobs, then the other role types.
    /// </summary>
    public static NebulaGhostRoleCategory Get(GhostRoleInfo role, IPrototypeManager prototypes)
    {
        var category = NebulaGhostRoleCategory.Other;

        if (role.RoleTypes != null)
        {
            foreach (var type in role.RoleTypes)
            {
                if (RoleTypes.TryGetValue(type, out var typeCategory) && typeCategory < category)
                    category = typeCategory;
            }
        }

        if (category == NebulaGhostRoleCategory.Antagonist)
            return category;

        if (role.RolePrototypes.Item1 is { } jobs
            && prototypes.TryIndex(CentralCommand, out var department)
            && jobs.Exists(job => department.Roles.Contains(job)))
        {
            return NebulaGhostRoleCategory.CentralCommand;
        }

        return category;
    }

    public static string GetName(NebulaGhostRoleCategory category)
    {
        return Loc.GetString(category switch
        {
            NebulaGhostRoleCategory.Antagonist => "nebula-ghost-roles-category-antagonist",
            NebulaGhostRoleCategory.CentralCommand => "nebula-ghost-roles-category-central-command",
            NebulaGhostRoleCategory.FreeAgent => "nebula-ghost-roles-category-free-agent",
            NebulaGhostRoleCategory.Silicon => "nebula-ghost-roles-category-silicon",
            NebulaGhostRoleCategory.Familiar => "nebula-ghost-roles-category-familiar",
            _ => "nebula-ghost-roles-category-other",
        });
    }

    public static Texture GetIcon(NebulaGhostRoleCategory category)
    {
        return NebulaIcons.Get(category switch
        {
            NebulaGhostRoleCategory.Antagonist => "Ghost/skull",
            NebulaGhostRoleCategory.CentralCommand => "Ghost/shield_person",
            NebulaGhostRoleCategory.FreeAgent => "Ghost/theater_comedy",
            NebulaGhostRoleCategory.Silicon => "Departments/smart_toy",
            NebulaGhostRoleCategory.Familiar => "Ghost/pets",
            _ => "Departments/person",
        });
    }

    /// <summary>
    /// Accent style class of the category, null for the plain ones.
    /// </summary>
    public static string? GetAccent(NebulaGhostRoleCategory category)
    {
        return category switch
        {
            NebulaGhostRoleCategory.Antagonist => NebulaStyleClass.AccentError,
            NebulaGhostRoleCategory.CentralCommand => NebulaStyleClass.AccentInfo,
            _ => null,
        };
    }
}
