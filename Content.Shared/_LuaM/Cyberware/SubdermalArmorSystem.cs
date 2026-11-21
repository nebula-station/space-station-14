// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Server/_LuaM/Cyberware/SubdermalArmorSystem.cs

using Content.Shared.Damage;
using Content.Shared.Damage.Systems;

namespace Content.Shared._LuaM.Cyberware;

/// <summary>
/// Applies the protection of the subdermal armor in the body to incoming damage.
/// </summary>
public sealed partial class SubdermalArmorSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnDamageModify(Entity<SubdermalArmorComponent> ent, ref DamageModifyEvent args)
    {
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, ent.Comp.Modifiers);
    }
}
