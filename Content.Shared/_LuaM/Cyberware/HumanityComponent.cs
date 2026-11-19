// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Shared/_LuaM/Cyberware/HumanityComponent.cs

using Content.Shared.Damage.Prototypes;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._LuaM.Cyberware;

/// <summary>
/// How much cyberware a creature can take before it starts losing itself.
/// Every point over the limit gives a chance of cyberpsychosis when the creature takes physical damage.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class HumanityComponent : Component
{
    [DataField]
    public float MaxHumanity = 6f;

    /// <summary>
    /// Extra humanity by species, a racial feature.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<SpeciesPrototype>, float> SpeciesBonus = [];

    /// <summary>
    /// Chance of cyberpsychosis for every humanity point over the limit.
    /// </summary>
    [DataField]
    public float ChancePerPoint = 0.25f;

    /// <summary>
    /// Damage of this group can trigger cyberpsychosis.
    /// </summary>
    [DataField]
    public ProtoId<DamageGroupPrototype> TriggerGroup = "Brute";

    [DataField]
    public TimeSpan PsychosisDuration = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Time after a cyberpsychosis ends before another one can start.
    /// </summary>
    [DataField]
    public TimeSpan PsychosisCooldown = TimeSpan.FromMinutes(10);

    [ViewVariables, AutoPausedField]
    public TimeSpan NextPsychosis;

    /// <summary>
    /// Faction the creature joins while in cyberpsychosis, hostile to everyone.
    /// </summary>
    [DataField]
    public ProtoId<NpcFactionPrototype> PsychosisFaction = "SimpleHostile";

    /// <summary>
    /// Entity the player watches through while the body is out of their control.
    /// </summary>
    [DataField]
    public EntProtoId ViewerProto = "CyberpsychosisViewer";
}

/// <summary>
/// Humanity cost of a piece of cyberware: an organ, a body part or a subdermal implant.
/// </summary>
[RegisterComponent]
public sealed partial class CyberwareHumanityComponent : Component
{
    [DataField]
    public float Cost;

    /// <summary>
    /// Extra cost from autosurgeon upgrades of this part, by humanity key.
    /// </summary>
    [DataField]
    public Dictionary<string, float> Upgrades = [];
}

/// <summary>
/// Head chip that raises the humanity limit of its owner.
/// </summary>
[RegisterComponent]
public sealed partial class HumanityChipComponent : Component
{
    [DataField]
    public float Bonus = 2f;
}

/// <summary>
/// Raised on a creature after an autosurgeon installed cyberware into it.
/// </summary>
[ByRefEvent]
public readonly record struct CyberwareInstalledEvent;
