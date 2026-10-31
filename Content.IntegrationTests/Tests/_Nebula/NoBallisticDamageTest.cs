// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Reflection;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Nebula;

/// <summary>
/// Ballistic damage is not used: no entity component deals or resists it and no modifier set mentions it.
/// Healing it is allowed, leftover healing specifiers are harmless.
/// </summary>
[TestFixture]
[TestOf(typeof(DamageTypePrototype))]
public sealed class NoBallisticDamageTest
{
    private static readonly ProtoId<DamageTypePrototype> Ballistic = "Ballistic";

    [Test]
    public async Task NoBallisticInWeaponsAndArmor()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var protoMan = server.ProtoMan;

        await server.WaitAssertion(() =>
        {
            List<string> errors = [];

            foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (proto.Abstract)
                    continue;

                foreach (var (name, entry) in proto.Components)
                {
                    foreach (var field in entry.Component.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        var source = $"{name}.{field.Name}";
                        switch (field.GetValue(entry.Component))
                        {
                            case DamageSpecifier damage:
                                CheckDamage(errors, proto.ID, source, damage);
                                break;
                            case DamageModifierSet modifiers:
                                CheckModifiers(errors, proto.ID, source, modifiers);
                                break;
                        }
                    }
                }
            }

            foreach (var set in protoMan.EnumeratePrototypes<DamageModifierSetPrototype>())
            {
                CheckModifiers(errors, set.ID, "damageModifierSet", set);
            }

            Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        });

        await pair.CleanReturnAsync();
    }

    private static void CheckDamage(List<string> errors, string id, string source, DamageSpecifier damage)
    {
        if (damage.DamageDict.TryGetValue(Ballistic, out var amount) && amount > 0)
            errors.Add($"{id}: {source} deals {Ballistic} damage");
    }

    private static void CheckModifiers(List<string> errors, string id, string source, DamageModifierSet modifiers)
    {
        if (modifiers.Coefficients.ContainsKey(Ballistic)
            || modifiers.FlatReductions.ContainsKey(Ballistic))
            errors.Add($"{id}: {source} modifies {Ballistic} damage");
    }
}
