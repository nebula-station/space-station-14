// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/93847a07a4b0b5890003c074e4768c5ba828543a/Content.IntegrationTests/Tests/_Arcane/Medical/ConcussionTest.cs

#nullable enable
using System.Linq;
using System.Numerics;
using Content.Shared._Arcane.Medical.Concussion;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Flash;
using Content.Shared.Speech.Components;
using Content.Shared.StatusEffectNew;
using Content.Trauma.Common.Language.Systems;
using Content.Trauma.Shared.Language.Events;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using ClientConcussion = Content.Client._Arcane.Medical.Concussion.ConcussionSystem;
using ServerConcussion = Content.Server._Arcane.Medical.Concussion.ConcussionSystem;
using ConcussionOverlay = Content.Client._Arcane.Medical.Concussion.ConcussionOverlay;
using ContentAudioSystem = Content.Client.Audio.ContentAudioSystem;
using FlashAfterimageOverlay = Content.Client._Arcane.Flash.FlashAfterimageOverlay;
using RobustAudioSystem = Robust.Client.Audio.AudioSystem;

namespace Content.IntegrationTests.Tests._Arcane.Medical;

[TestOf(typeof(ServerConcussion))]
public sealed class ConcussionTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly ServerConcussion _concussion = null!;
    [SidedDependency(Side.Server)] private readonly StatusEffectsSystem _effects = null!;
    [SidedDependency(Side.Server)] private readonly CommonLanguageSystem _languages = null!;
    [SidedDependency(Side.Client)] private readonly ClientConcussion _presentation = null!;
    [SidedDependency(Side.Client)] private readonly IOverlayManager _overlays = null!;
    [SidedDependency(Side.Client)] private readonly ContentAudioSystem _clientAudio = null!;
    [SidedDependency(Side.Client)] private readonly RobustAudioSystem _audio = null!;
    private static readonly EntProtoId Speech = "StatusEffectConcussedSpeech";

    [Test]
    public async Task ChangingBodyAndRemovingTheComponentDoNotLeakEffects()
    {
        await Pair.CreateTestMap();
        EntityUid patient = default;
        EntityUid healthy = default;
        var language = CommonLanguageSystem.FallbackLanguagePrototype;
        try
        {
            await Server.WaitPost(() =>
            {
                patient = SSpawnAtPosition("MobHuman", TestMap!.GridCoords);
                healthy = SSpawnAtPosition("MobHuman", TestMap.GridCoords);
                var comp = SComp<ConcussionThresholdComponent>(patient);
                comp.HealRate = 0;
                _concussion.AddConcussionDamage((patient, comp), comp.Thresholds.First(entry => entry.Value == ConcussionState.Overwhelmed).Key);
                _effects.TryUpdateStatusEffectDuration(patient, SharedFlashSystem.FlashedKey, TimeSpan.FromMinutes(1));
                SEntMan.EnsureComponent<SlurredAccentComponent>(patient);
                var impaired = new DetermineEntityLanguagesEvent();
                impaired.UnderstoodLanguages.Add(language);
                SEntMan.EventBus.RaiseLocalEvent(patient, ref impaired);
                Assert.That(impaired.UnderstoodLanguages, Is.Empty);
                Assert.That(_languages.CanUnderstand(patient, CommonLanguageSystem.UniversalPrototype), Is.True);
                Assert.That(_languages.CanUnderstand(patient, CommonLanguageSystem.PsychomanticPrototype), Is.True);
                Server.PlayerMan.SetAttachedEntity(ServerSession, patient);
            });
            await RunSeconds(0.2f);
            await AssertOverlays(true, true);
            await Client.WaitPost(() =>
            {
                Assert.That(_audio.GetOcclusion(_audio.GetListenerCoordinates(), Vector2.Zero, 0f),
                    Is.EqualTo(float.PositiveInfinity));
            });
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, healthy));
            await RunSeconds(0.2f);
            await AssertOverlays(false, false);
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, patient));
            await RunSeconds(0.2f);
            await AssertOverlays(true, true);
            await Server.WaitPost(() => SEntMan.RemoveComponent<ConcussionThresholdComponent>(patient));
            await RunSeconds(0.2f);
            await AssertOverlays(false, true);
            await Server.WaitPost(() =>
            {
                Assert.That(_effects.TryGetStatusEffect(patient, Speech, out _), Is.False);
                Assert.That(SHasComp<SlurredAccentComponent>(patient), Is.True);
                var restored = new DetermineEntityLanguagesEvent();
                restored.UnderstoodLanguages.Add(language);
                SEntMan.EventBus.RaiseLocalEvent(patient, ref restored);
                Assert.That(restored.UnderstoodLanguages, Does.Contain(language));
            });
        }
        finally
        {
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, null));
        }
        await RunSeconds(0.2f);
        await AssertOverlays(false, false);
    }

    private Task AssertOverlays(bool concussion, bool flash) => Client.WaitPost(() =>
    {
        _presentation.FrameUpdate(0f);
        Assert.That(_overlays.HasOverlay<ConcussionOverlay>(), Is.EqualTo(concussion));
        Assert.That(_overlays.HasOverlay<FlashAfterimageOverlay>(), Is.EqualTo(flash));
        Assert.That(_clientAudio.ListenerOcclusion > 0f, Is.EqualTo(concussion));
    });
}
