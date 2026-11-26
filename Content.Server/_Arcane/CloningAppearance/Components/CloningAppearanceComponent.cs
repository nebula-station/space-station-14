// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Server/CloningAppearance/Components/CloningAppearanceComponent.cs

using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Arcane.CloningAppearance;

[RegisterComponent]
public sealed partial class CloningAppearanceComponent : Component
{
    [DataField("components")]
    [AlwaysPushInheritance]
    public ComponentRegistry Components { get; private set; } = new();

    [DataField("startingGear")]
    public ProtoId<StartingGearPrototype>? Gear;
}
