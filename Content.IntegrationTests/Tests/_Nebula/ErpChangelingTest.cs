// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Shared.Changeling.Components;
using Content.Server._Nebula.ERP;
using Content.Server.Polymorph.Systems;
using Content.Server.Store.Conditions;
using Content.Shared._Nebula.Changeling;
using Content.Shared._Nebula.ERP;
using Content.Shared._Nebula.ErpPanel;
using Content.Shared.Actions;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.GameTicking.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Polymorph;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Trauma.Shared._Nebula.ErpChangeling;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Nebula;

[TestFixture]
public sealed class ErpChangelingTest : GameTest
{
    private static readonly EntProtoId Urist = "MobHuman";
    private static readonly EntProtoId AbsorbDna = "ActionAbsorbDNA";
    private static readonly EntProtoId ExtractDna = "ActionStingExtractDNA";
    private static readonly EntProtoId CrocinSting = "ActionStingCrocin";
    private static readonly EntProtoId ErpChangelingRole = "MindRoleErpChangeling";
    private static readonly ProtoId<ReagentPrototype> Crocin = "Crocin";
    private static readonly ProtoId<CurrencyPrototype> EvolutionPoint = "EvolutionPoint";
    private static readonly ProtoId<ErpConsentTagPrototype> Erp = "Erp";
    private static readonly ProtoId<ErpConsentTagPrototype> Rape = "Rape";
    private static readonly ProtoId<ErpConsentTagPrototype> HornyAntagVictim = "HornyAntagVictim";

    private static readonly string[] BlockedListings =
    [
        "EvolutionMenuStingFakeArmblade",
        "EvolutionMenuUtilityInstinct",
        "EvolutionMenuUtilityAbsorbBiomatter",
        "EvolutionMenuUtilityBiodegrade",
        "EvolutionMenuUtilityOverdose",
        "EvolutionMenuUtilityFleshmend",
        "EvolutionMenuUtilityLastResort",
        "EvolutionMenuUtilityLesserForm",
    ];

    [SidedDependency(Side.Server)] private SharedActionsSystem _actions = default!;
    [SidedDependency(Side.Server)] private SharedSolutionContainerSystem _solution = default!;
    [SidedDependency(Side.Server)] private ArousalSystem _arousal = default!;
    [SidedDependency(Side.Server)] private SharedMindSystem _mind = default!;
    [SidedDependency(Side.Server)] private SharedRoleSystem _role = default!;
    [SidedDependency(Side.Server)] private PolymorphSystem _polymorph = default!;

    /// <summary>
    /// The partials and the ERP store are what keep combat abilities away from the ERP changeling.
    /// </summary>
    [Test]
    public async Task PrototypesAreWired()
    {
        await Server.WaitAssertion(() =>
        {
            var extended = SProtoMan.Index<GamePresetPrototype>("Extended");
            Assert.That(extended.Rules, Does.Contain(new EntProtoId("ErpChangelingSpawn")),
                "The ERP changeling rule isn't in the Extended preset, is the Nebula partials directory registered?");

            foreach (var id in BlockedListings)
            {
                var listing = SProtoMan.Index<ListingPrototype>(id);
                var blocked = listing.Conditions?.OfType<BuyerAntagCondition>()
                    .Any(c => c.Blacklist?.Contains("ErpChangeling") == true) ?? false;
                Assert.That(blocked, $"{id} can be bought by the ERP changeling");
            }

            var role = SProtoMan.Index(ErpChangelingRole);
            Assert.That(role.TryComp<StoreComponent>(out var store, SEntMan.ComponentFactory));
            Assert.That(store!.Categories, Does.Not.Contain(new ProtoId<StoreCategoryPrototype>("ChangelingAbilityCombat")));
        });
    }

    [Test]
    public async Task StartsWithoutAbsorb()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var ling = MakeErpChangeling(map.GridCoords);
            Assert.That(SHasComp<ErpChangelingComponent>(ling), "The ERP starting evolution wasn't applied");

            var actions = _actions.GetActions(ling).Select(a => SPrototype(a)?.ID).ToList();
            Assert.That(actions, Does.Not.Contain(AbsorbDna.Id));
            Assert.That(actions, Does.Contain(ExtractDna.Id));
        });
    }

    [Test]
    public async Task OnlyHornyAntagVictims()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var ling = MakeErpChangeling(map.GridCoords);
            var action = SSpawn(CrocinSting, map.GridCoords);

            // the rape tag doesn't matter, only being a horny antag victim does
            var victim = SSpawn(Urist, map.GridCoords);
            SetConsent(victim, HornyAntagVictim, ErpConsent.Yes);
            SetConsent(victim, Rape, ErpConsent.No);
            Assert.That(TrySting(ling, victim, action), "Stinging a horny antag victim was blocked");
            Assert.That(TryInteract(ling, victim), "An ERP interaction with a horny antag victim was blocked");

            var notVictim = SSpawn(Urist, map.GridCoords);
            SetConsent(notVictim, HornyAntagVictim, ErpConsent.Ask);
            SetConsent(notVictim, Rape, ErpConsent.Yes);
            Assert.That(TrySting(ling, notVictim, action), Is.False, "Stinging someone who isn't a horny antag victim wasn't blocked");
            Assert.That(TryInteract(ling, notVictim), Is.False, "An ERP interaction with someone who isn't a horny antag victim wasn't blocked");

            // the tag is No unless the character changed it
            var untouched = SSpawn(Urist, map.GridCoords);
            Assert.That(TrySting(ling, untouched, action), Is.False, "Stinging someone with default tags wasn't blocked");
        });
    }

    /// <summary>
    /// Transforming polymorphs the changeling into a new body, the consent checks have to come along.
    /// </summary>
    [Test]
    public async Task KeepsConsentChecksAfterTransform()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var ling = MakeErpChangeling(map.GridCoords);
            var config = new PolymorphConfiguration { Entity = Urist, Forced = true };
            if (_polymorph.PolymorphEntity(ling, config) is not { } newBody)
            {
                Assert.Fail("Polymorph failed");
                return;
            }

            Assert.That(SHasComp<ErpChangelingComponent>(newBody), "The transformed body lost the ERP changeling component");

            var action = SSpawn(CrocinSting, map.GridCoords);
            var unwilling = SSpawn(Urist, map.GridCoords);
            Assert.That(TrySting(newBody, unwilling, action), Is.False, "A transformed ERP changeling could sting someone whose rape tag is No");
        });
    }

    [Test]
    public async Task PartnersGiveEvolutionPointsOnce()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var ling = MakeErpChangeling(map.GridCoords);
            var lingMind = _mind.CreateMind(null);
            _mind.TransferTo(lingMind, ling, mind: lingMind.Comp);
            _role.MindAddRole(lingMind, ErpChangelingRole, lingMind.Comp);

            Assert.That(_role.MindHasRole<ChangelingRoleComponent>(lingMind.Owner, out var role));
            var store = SEntMan.GetComponent<StoreComponent>(role!.Value.Owner);
            var before = store.Balance[EvolutionPoint];

            var partner = SSpawn(Urist, map.GridCoords);
            Orgasm(partner, ling);
            Assert.That(store.Balance[EvolutionPoint], Is.EqualTo(before + 2), "The first orgasm of a partner gave no evolution points");

            Orgasm(partner, ling);
            Assert.That(store.Balance[EvolutionPoint], Is.EqualTo(before + 2), "The same partner gave evolution points twice");
        });
    }

    [Test]
    public async Task CrocinNeedsConsent()
    {
        var map = await Pair.CreateTestMap();
        var willing = EntityUid.Invalid;
        var unwilling = EntityUid.Invalid;

        await Server.WaitAssertion(() =>
        {
            willing = SSpawn(Urist, map.GridCoords);
            unwilling = SSpawn(Urist, map.GridCoords);
            SetConsent(unwilling, Erp, ErpConsent.No);

            foreach (var mob in new[] { willing, unwilling })
            {
                SEnsureComp<ArousalComponent>(mob);
                Assert.That(_solution.TryGetInjectableSolution(mob, out var soln, out _));
                Assert.That(_solution.TryAddReagent(soln!.Value, Crocin, 10));
            }
        });

        await RunSeconds(5);

        await Server.WaitAssertion(() =>
        {
            var willingArousal = _arousal.GetArousal(SEntMan.GetComponent<ArousalComponent>(willing));
            var unwillingArousal = _arousal.GetArousal(SEntMan.GetComponent<ArousalComponent>(unwilling));
            Assert.That(willingArousal, Is.GreaterThan(0f), "Crocin didn't arouse someone who allows ERP");
            Assert.That(willingArousal, Is.LessThanOrEqualTo(30f), "Crocin went past its cap");
            Assert.That(unwillingArousal, Is.EqualTo(0f), "Crocin aroused someone whose ERP tag is No");
        });
    }

    private EntityUid MakeErpChangeling(EntityCoordinates coords)
    {
        var ling = SSpawn(Urist, coords);
        SEntMan.AddComponent(ling, new ChangelingComponent { EvolutionsProto = "ErpChangeling" });
        return ling;
    }

    private void Orgasm(EntityUid uid, EntityUid partner)
    {
        var recent = SEnsureComp<ErpRecentPartnerComponent>(uid);
        recent.Partner = partner;
        recent.Time = Server.Timing.CurTime;

        var ev = new ArousalOrgasmEvent(null);
        SEntMan.EventBus.RaiseLocalEvent(uid, ref ev);
    }

    private void SetConsent(EntityUid uid, ProtoId<ErpConsentTagPrototype> tag, ErpConsent consent)
    {
        var profile = SEntMan.GetComponent<HumanoidProfileComponent>(uid);
        profile.DetailExaminableData = profile.DetailExaminableData.CopyWithConsent(tag, consent);
    }

    private bool TryInteract(EntityUid ling, EntityUid target)
    {
        var ev = new ErpPanelInteractionAttemptEvent(target, Erotic: true);
        SEntMan.EventBus.RaiseLocalEvent(ling, ref ev);
        return !ev.Cancelled;
    }

    private bool TrySting(EntityUid ling, EntityUid target, EntityUid action)
    {
        var ev = new ChangelingStingAttemptEvent(target, action);
        SEntMan.EventBus.RaiseLocalEvent(ling, ref ev);
        return !ev.Cancelled;
    }
}
