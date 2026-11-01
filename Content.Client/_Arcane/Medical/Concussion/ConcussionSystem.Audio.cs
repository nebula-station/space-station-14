// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/93847a07a4b0b5890003c074e4768c5ba828543a/Content.Arcane.Client/Medical/Concussion/ConcussionSystem.Audio.cs

using Content.Client.Audio;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Client._Arcane.Medical.Concussion;

public sealed partial class ConcussionSystem
{
    [Dependency] private ContentAudioSystem _contentAudio = default!;
    [Dependency] private IAudioManager _audioManager = default!;

    private readonly Dictionary<EntityUid, AudioStream> _tinnitusStreams = [];

    // The source's filter gain is exp(-occlusion / 10); invert it for linear hearing recovery.
    private const float HearingOcclusionScale = 10f;
    private const int TinnitusFrequency = 3000;
    private const int TinnitusSampleRate = 48000;

    private EntityUid? PlayTinnitus(SoundSpecifier? sound, float volume)
    {
        var audioParams = AudioParams.Default.WithLoop(true).WithVolume(volume);
        if (sound != null)
            return _audio.PlayGlobal(sound, Filter.Local(), false, audioParams)?.Entity;

        // A whole number of periods makes the short loop seamless; only concussion controls its volume.
        var samples = new short[TinnitusSampleRate / 10];
        for (var i = 0; i < samples.Length; i++)
            samples[i] = (short) (short.MaxValue * 0.32 * Math.Sin(Math.Tau * TinnitusFrequency * i / TinnitusSampleRate));

        var stream = _audioManager.LoadAudioRaw(samples, 1, TinnitusSampleRate, "Concussion tinnitus");
        if (_audio.PlayGlobal(stream, null, audioParams) is not { } playing)
        {
            stream.Dispose();
            return null;
        }

        _tinnitusStreams.Add(playing.Entity, stream);
        return playing.Entity;
    }

    private void InitializeHearing()
    {
        EntityManager.EntityDeleted += OnAudioEntityDeleted;
    }

    private void OnAudioEntityDeleted(Entity<MetaDataComponent> ent)
    {
        // AudioComponent shutdown has released the source before its PCM buffer is disposed.
        if (_tinnitusStreams.Remove(ent.Owner, out var stream))
            stream.Dispose();
    }

    private void UpdateHearing(float intensity, float recoveryThreshold)
    {
        if (intensity <= 0f)
            _contentAudio.ListenerOcclusion = 0f;
        else if (intensity >= recoveryThreshold)
            _contentAudio.ListenerOcclusion = float.PositiveInfinity;
        else
            _contentAudio.ListenerOcclusion = -HearingOcclusionScale * MathF.Log(1f - intensity / recoveryThreshold);
    }

    private void ResetHearing()
    {
        _contentAudio.ListenerOcclusion = 0f;
    }

    private void ShutdownHearing()
    {
        EntityManager.EntityDeleted -= OnAudioEntityDeleted;
        foreach (var (uid, stream) in _tinnitusStreams)
        {
            Del(uid);
            stream.Dispose();
        }
        _tinnitusStreams.Clear();
    }
}
