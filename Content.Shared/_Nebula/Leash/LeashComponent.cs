// SPDX-License-Identifier: AGPL-3.0-or-later

// Ported from Radiant Sector: https://github.com/TetrisZz/frontier-Radiant

using Robust.Shared.Utility;

namespace Content.Shared._Nebula.Leash;

/// <summary>
/// Item that can leash other humanoids while held.
/// </summary>
[RegisterComponent]
public sealed partial class LeashComponent : Component
{
    [DataField]
    public float MaxForce = 380f;

    [DataField]
    public float Frequency = 12f;

    [DataField]
    public float DampingRatio = 2.5f;

    [DataField]
    public float MassLimit = 200f;

    /// <summary>
    /// Longest length the leash can be loosened to. It snaps a bit past this.
    /// </summary>
    [DataField]
    public float MaxDistance = 14f;

    // Nebula start
    /// <summary>
    /// Shortest length the leash can be tightened to.
    /// </summary>
    [DataField]
    public float MinDistance = 1f;

    /// <summary>
    /// Step used by the tighten / loosen verbs.
    /// </summary>
    [DataField]
    public float DistanceStep = 0.5f;
    // Nebula end

    [DataField]
    public int MaxTargets = 3;

    [DataField]
    public TimeSpan AttachDelay = TimeSpan.FromSeconds(3); // Nebula edit

    // Nebula start
    [DataField]
    public SpriteSpecifier RopeSprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Weapons/Guns/Launchers/grappling_gun.rsi"), "rope");

    [DataField]
    public TimeSpan YankCooldown = TimeSpan.FromSeconds(5);
    // Nebula end
}
