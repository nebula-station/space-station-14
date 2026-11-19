// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Shared/_LuaM/Cyberware/SubdermalArmorComponent.cs

using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._LuaM.Cyberware;

/// <summary>
/// Damage protection of a subdermal armor organ.
/// Put on the body through the organ's onAdd, so it is removed together with the armor.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SubdermalArmorComponent : Component
{
    [DataField(required: true)]
    public DamageModifierSet Modifiers = new();
}
