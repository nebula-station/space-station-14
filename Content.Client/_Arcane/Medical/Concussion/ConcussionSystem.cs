// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/93847a07a4b0b5890003c074e4768c5ba828543a/Content.Arcane.Client/Medical/Concussion/ConcussionSystem.cs

using Content.Shared._Arcane.Medical.Concussion;
using Content.Shared.GameTicking;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Arcane.Medical.Concussion;

public sealed partial class ConcussionSystem : SharedConcussionSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlays = default!;
    [Dependency] private AudioSystem _audio = default!;

    private ConcussionOverlay _overlay = default!;
    private EntityUid? _sound;
    private bool _hasOverlay;
    private bool _refreshRequired;

    public override void Initialize()
    {
        base.Initialize();
        InitializeHearing();
        _overlay = new ConcussionOverlay();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        if (_refreshRequired)
        {
            _refreshRequired = false;
            Refresh();
        }
        if (_hasOverlay && _overlay.TargetIntensity == 0 && _overlay.SmoothedIntensity == 0)
        {
            _overlays.RemoveOverlay(_overlay);
            _hasOverlay = false;
        }
    }

    [SubscribeLocalEvent]
    private void OnStateHandled(Entity<ConcussionThresholdComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Owner == _player.LocalEntity)
            _refreshRequired = true;
    }

    private void Refresh()
    {
        if (!TryComp<ConcussionThresholdComponent>(_player.LocalEntity, out var concussion))
        {
            Reset();
            return;
        }
        var intensity = GetIntensity(concussion);
        UpdateHearing(intensity, concussion.HearingRecoveryThreshold);
        _overlay.TargetIntensity = intensity;
        if (_overlay.TargetIntensity > 0 && !_hasOverlay)
        {
            _overlays.AddOverlay(_overlay);
            _hasOverlay = true;
        }
        if (concussion.CurrentState == ConcussionState.Sane)
        {
            _sound = _audio.Stop(_sound);
        }
        else
        {
            var volume = -7f + 20f * MathF.Log10(Math.Max(intensity, 0.01f));
            if (!Exists(_sound))
                _sound = PlayTinnitus(concussion.Sound, volume);
            else
                _audio.SetVolume(_sound, volume);
        }
    }

    [SubscribeLocalEvent]
    private void OnAttached(LocalPlayerAttachedEvent args)
    {
        Reset();
        _refreshRequired = true;
    }

    [SubscribeLocalEvent]
    private void OnDetached(LocalPlayerDetachedEvent args)
    {
        Reset();
        _refreshRequired = true;
    }

    [SubscribeNetworkEvent] // Nebula edit
    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        Reset();
    }

    [SubscribeLocalEvent]
    private void OnThresholdShutdown(Entity<ConcussionThresholdComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Owner == _player.LocalEntity)
            Reset();
    }

    private void Reset()
    {
        ResetHearing();
        _refreshRequired = false;
        _overlay.Reset();
        if (_hasOverlay)
            _overlays.RemoveOverlay(_overlay);
        _hasOverlay = false;
        _sound = _audio.Stop(_sound);
    }

    public override void Shutdown()
    {
        ShutdownHearing();
        Reset();
        _overlay.Dispose();
        base.Shutdown();
    }
}
