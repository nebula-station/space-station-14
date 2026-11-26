// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Shared/_Goobstation/Autosurgeon/AutoSurgeonComponent.cs

using Content.Medical.Common.Body;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._LuaM.Autosurgeon;

/// <summary>
/// Colossus cyberware installs of an autosurgeon, done before its own <c>AutoSurgeon</c> entries.
/// Organs go into slots the body part doesn't have yet, like the subdermal armor or the CNS implant slot.
/// </summary>
[RegisterComponent]
public sealed partial class CyberAutoSurgeonComponent : Component
{
    /// <summary>
    /// Body part that gets <see cref="NewOrgan"/> and keeps the cost of component upgrades.
    /// </summary>
    [DataField]
    public ProtoId<OrganCategoryPrototype> TargetPart = "Torso";

    /// <summary>
    /// Organ put into <see cref="TargetPart"/>, replacing the one in its slot.
    /// </summary>
    [DataField]
    public EntProtoId<OrganComponent>? NewOrgan;

    /// <summary>
    /// Puts <see cref="NewOrgan"/> on the side of the active hand instead of <see cref="TargetPart"/>:
    /// into the hand itself or the arm above it, whichever the organ can be a child of.
    /// </summary>
    [DataField]
    public bool ActiveSide;

    // Nebula start
    /// <summary>
    /// Organs put into <see cref="TargetPart"/> after <see cref="NewOrgan"/>, each replacing the one in its slot.
    /// </summary>
    [DataField]
    public List<EntProtoId<OrganComponent>> NewOrgans = [];
    // Nebula end

    /// <summary>
    /// Puts a new organ of this prototype into every body part of <see cref="PartType"/>,
    /// like a mantis blade into both arms. Parts that already have their slot taken are skipped.
    /// </summary>
    [DataField]
    public EntProtoId<OrganComponent>? OrganForEachPart;

    [DataField]
    public BodyPartType PartType = BodyPartType.Arm;

    /// <summary>
    /// Subdermal implant installed into the user.
    /// </summary>
    [DataField]
    public EntProtoId? Implant;

    /// <summary>
    /// Humanity cost of an upgrade that only adds components, like the muscle fibers.
    /// Installed organs and implants have their own cost instead.
    /// </summary>
    [DataField]
    public float HumanityCost;

    /// <summary>
    /// Upgrades with the same key share one humanity cost, so a better version replaces the cost instead of adding to it.
    /// The autosurgeon prototype is used if null.
    /// </summary>
    [DataField]
    public string? HumanityKey;
}
