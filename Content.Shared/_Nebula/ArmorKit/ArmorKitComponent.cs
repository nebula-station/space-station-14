// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.ArmorKit;

/// <summary>
/// One-use kit that permanently gives a piece of clothing the protection of <see cref="Source"/>.
/// </summary>
[RegisterComponent]
public sealed partial class ArmorKitComponent : Component
{
    /// <summary>
    /// Prototype whose protective components are copied onto the target.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Source;

    /// <summary>
    /// Clothing slots the kit can be applied to.
    /// </summary>
    [DataField(required: true)]
    public SlotFlags Slots;

    /// <summary>
    /// Word added before the name of the reinforced item.
    /// </summary>
    [DataField(required: true)]
    public LocId Prefix;

    [DataField(readOnly: true)]
    public Color PrefixColor = Color.FromHex("#DC143C");

    [DataField(readOnly: true)]
    public TimeSpan Delay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Components copied from <see cref="Source"/>. Ones the source lacks are removed from the target.
    /// </summary>
    [DataField(readOnly: true)]
    public HashSet<string> Components =
    [
        "Armor",
        "ExplosionResistance",
        "StaminaResistance",
        "ModifyDelayedKnockdown",
        "ArmorPointBlank",
    ];
}
