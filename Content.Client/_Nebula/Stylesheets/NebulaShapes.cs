// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.UserInterface.Shapes;

namespace Content.Client._Nebula.Stylesheets;

/// <summary>
/// Shapes used by Nebula sheetlets. Swap these to switch the whole UI between rounded and angled corners.
/// </summary>
public static class NebulaShapes
{
    public static readonly ShapeStyle Screen = ShapeStyle.Rounded(20f);
    public static readonly ShapeStyle Panel = ShapeStyle.Rounded(20f);
    public static readonly ShapeStyle Section = ShapeStyle.Rounded(14f);
    public static readonly ShapeStyle Card = ShapeStyle.Rounded(12f);
    public static readonly ShapeStyle Control = ShapeStyle.Rounded(6f);
    public static readonly ShapeStyle Bar = ShapeStyle.Pill;
}
