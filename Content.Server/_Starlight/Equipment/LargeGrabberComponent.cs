// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/TetrisZz/frontier-Radiant/blob/743dfcac03/Content.Server/_Starlight/Equipment/LargeGrabberComponent.cs

using System.Numerics;
using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server._Starlight.Equipment;

/// <summary>
/// Handheld version of the mech grabber: picks up unanchored objects and puts them down in front of the user.
/// </summary>
[RegisterComponent]
public sealed partial class LargeGrabberComponent : Component
{
    public const string ContainerId = "item-container";

    [DataField]
    public float GrabEnergyCost = 30;

    [DataField]
    public float GrabDelay = 2.5f;

    [DataField]
    public Vector2 DepositOffset = new(0, -1);

    [DataField]
    public int MaxContents = 10;

    [DataField]
    public SoundSpecifier GrabSound = new SoundPathSpecifier("/Audio/Mecha/sound_mecha_hydraulic.ogg");

    [DataField]
    public EntityWhitelist? Blacklist = new()
    {
        Components = ["WallMount", "Anomaly", "Mech", "MobState"],
    };

    /// <summary>
    /// Drop everything held when the grabber itself is moved to another container.
    /// </summary>
    [DataField]
    public bool DropOnContainerChange;

    [ViewVariables]
    public EntityUid? AudioStream;

    [ViewVariables]
    public Container ItemContainer = default!;

    [ViewVariables]
    public DoAfterId? DoAfter;
}
