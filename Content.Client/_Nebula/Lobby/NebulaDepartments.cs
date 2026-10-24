// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._Nebula.Lobby;

public static class NebulaDepartments
{
    private const string FallbackIcon = "work";

    private static readonly Dictionary<ProtoId<DepartmentPrototype>, string> Icons = new()
    {
        ["Command"] = "military_tech",
        ["CentralCommand"] = "account_balance",
        ["Security"] = "local_police",
        ["Engineering"] = "engineering",
        ["Medical"] = "medical_services",
        ["Science"] = "science",
        ["Cargo"] = "inventory_2",
        ["Service"] = "restaurant",
        ["Civilian"] = "person",
        ["Silicon"] = "smart_toy",
    };

    public static Texture GetIcon(DepartmentPrototype? department)
    {
        var name = department != null && Icons.TryGetValue(department.ID, out var icon) ? icon : FallbackIcon;
        return NebulaIcons.Get($"Departments/{name}");
    }

    /// <summary>
    /// Null leaves the icon color to its style class.
    /// </summary>
    public static Color? GetColor(NebulaPalette palette, DepartmentPrototype? department)
    {
        return department == null ? null : palette.Harmonize(department.Color);
    }
}
