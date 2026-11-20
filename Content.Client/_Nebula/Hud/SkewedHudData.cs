// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mobs;

namespace Content.Client._Nebula.Hud;

/// <summary>
/// Task priority, highest first. Tasks of the same priority keep the order they were added in.
/// </summary>
/// <summary>
/// A line in the task section of the HUD, owned by the module that set it.
/// </summary>
public struct SkewedHudTask
{
    /// <summary>
    /// The module the task came from, see <see cref="SkewedHudSystem.SetTasks"/>.
    /// </summary>
    public required string Source;
    public required string Label;
    public required string Text;

    /// <summary>
    /// Shown in a tooltip while the mouse is over the task.
    /// </summary>
    public string? Description;

    /// <summary>
    /// Lower goes higher on the HUD.
    /// </summary>
    public int Order;

    /// <summary>
    /// Drawn in the alert color of the profile and pulses.
    /// </summary>
    public bool Urgent;
    public TimeSpan ChangedAt;
}

/// <summary>
/// A <c>LABEL :: value</c> line of a module section.
/// </summary>
public struct SkewedHudLine
{
    public required string Label;
    public required string Value;

    /// <summary>
    /// Color of the value, the accent of the profile when null.
    /// </summary>
    public Color? Color;

    /// <summary>
    /// Color of the label, the dim accent of the profile when null.
    /// </summary>
    public Color? LabelColor;
    public bool Pulse;

    /// <summary>
    /// Data from the servers: freezes and breaks up while the link is lost.
    /// </summary>
    public bool Lags;
    public TimeSpan ChangedAt;
}

/// <summary>
/// A block of lines a module puts on the HUD, like the squad of the security module.
/// </summary>
public sealed class SkewedHudSection
{
    public required string Source;
    public required string Id;

    /// <summary>
    /// Lower goes higher. Sections below <see cref="SkewedHudSystem.InfoOrder"/> go above the location, status
    /// and code block, the rest under it.
    /// </summary>
    public required int Order;

    /// <summary>
    /// Drawn in the top right corner instead of the left column.
    /// </summary>
    public bool Right;
    public readonly List<SkewedHudLine> Lines = [];
}
