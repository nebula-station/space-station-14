// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared._Nebula.ArmorKit;
using Content.Shared.Armor;
using Content.Shared.Clothing.Components;
using Content.Shared.Storage;

namespace Content.IntegrationTests.Tests._Nebula;

/// <summary>
/// Checks that every armor kit points to real armor and that applying one copies its protection exactly once.
/// </summary>
[TestFixture]
[TestOf(typeof(ArmorKitSystem))]
public sealed class ArmorKitTest : InteractionTest
{
    private static readonly EntProtoId SourceVest = "ClothingOuterArmorBasic";

    [Test]
    public async Task KitPrototypesAreValid()
    {
        var loc = Server.ResolveDependency<ILocalizationManager>();

        await Server.WaitAssertion(() =>
        {
            List<string> errors = [];

            var kits = ProtoMan.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract && p.HasComp<ArmorKitComponent>(Factory))
                .OrderBy(p => p.ID)
                .ToList();

            Assert.That(kits, Is.Not.Empty, "No armor kit prototypes found.");

            foreach (var kit in kits)
            {
                kit.TryComp<ArmorKitComponent>(out var comp, Factory);

                if (!loc.HasString(comp.Prefix))
                    errors.Add($"Kit '{kit.ID}': missing localization '{comp.Prefix}'.");

                if (!ProtoMan.TryIndex(comp.Source, out var source))
                {
                    errors.Add($"Kit '{kit.ID}': unknown source '{comp.Source}'.");
                    continue;
                }

                if (!source.HasComp<ArmorComponent>(Factory))
                    errors.Add($"Kit '{kit.ID}': source '{source.ID}' has no armor.");

                if (!source.TryComp<ClothingComponent>(out var clothing, Factory)
                    || (clothing.Slots & comp.Slots) == 0)
                    errors.Add($"Kit '{kit.ID}': source '{source.ID}' isn't worn in slots {comp.Slots}.");

                foreach (var name in comp.Components)
                {
                    if (!Factory.TryGetRegistration(name, out _))
                        errors.Add($"Kit '{kit.ID}': unknown component '{name}'.");
                }
            }

            Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        });
    }

    [Test]
    public async Task KitAppliesOnce()
    {
        await SpawnTarget("ClothingOuterCoatJensen");
        await InteractUsing("ArmorKitSecurity");

        await Server.WaitAssertion(() =>
        {
            var target = STarget.Value;
            Assert.That(SEntMan.HasComponent<ArmorKitAppliedComponent>(target));
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(target).EntityName,
                Does.StartWith(Loc.GetString("armor-kit-prefix-reinforced")));

            ProtoMan.Index(SourceVest).TryComp<ArmorComponent>(out var source, Factory);
            var armor = SEntMan.GetComponent<ArmorComponent>(target);
            Assert.That(armor.Modifiers.Coefficients, Is.EquivalentTo(source.Modifiers.Coefficients));
        });

        await InteractUsing("ArmorKitHoS");

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityQuery<ArmorKitComponent>().Any(), "Second kit was used up.");
            Assert.That(SEntMan.GetComponent<ArmorKitAppliedComponent>(STarget.Value).Prefix.Id,
                Is.EqualTo("armor-kit-prefix-reinforced"));
        });
    }

    [Test]
    public async Task KitStoresIntoBackpack()
    {
        await SpawnTarget("ClothingBackpack");
        await InteractUsing("ArmorKitSecurity");

        await Server.WaitAssertion(() =>
        {
            var storage = SEntMan.GetComponent<StorageComponent>(STarget!.Value);
            Assert.That(storage.Container.ContainedEntities.Any(SEntMan.HasComponent<ArmorKitComponent>),
                "Kit wasn't stored into the backpack.");
        });
    }
}
