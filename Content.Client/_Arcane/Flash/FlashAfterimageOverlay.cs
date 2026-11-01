// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/93847a07a4b0b5890003c074e4768c5ba828543a/Content.Client/Flash/FlashOverlay.cs

using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.Flash;
using Content.Shared.Flash.Components;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Arcane.Flash;

/// <summary>
/// Replaces the upstream flash overlay: draws an overexposed copy of the last visible frame.
/// Both accessibility modes blind equally, and without a frame the screen is plain white.
/// </summary>
public sealed partial class FlashAfterimageOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "ArcaneFlashedEffect";

    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private IClyde _clyde = default!;

    private readonly StatusEffectsSystem _statusSys;
    private readonly ShaderInstance _shader;

    private bool _reducedMotion;
    private float _percentComplete = 1f;
    private IRenderTexture? _snapshot;
    private TimeSpan? _effectEnd;
    private TimeSpan _visualStart;
    private float _elapsed;
    private float _duration = 1f;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public FlashAfterimageOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index(Shader).InstanceUnique();
        _statusSys = _entityManager.System<StatusEffectsSystem>();
        ZIndex = 15;

        _configManager.OnValueChanged(CCVars.DisableFlashEffect, OnReducedMotionChanged, invokeImmediately: true);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        if (_playerManager.LocalEntity is not { } player
            || !_statusSys.HasEffectComp<FlashedStatusEffectComponent>(player)
            || !_statusSys.TryGetTime(player, SharedFlashSystem.FlashedKey, out var time)
            || time.StartEffectTime is not { } start
            || time.EndEffectTime is not { } end)
            return;

        var curTime = _timing.CurTime;

        // A longer flash restarts the visual phase but keeps the frozen frame.
        if (_effectEnd == null)
            _visualStart = start;
        else if (end > _effectEnd)
            _visualStart = curTime;

        _effectEnd = end;
        _duration = Math.Max(0.001f, (float) (end - _visualStart).TotalSeconds);
        _elapsed = Math.Max(0f, (float) (curTime - _visualStart).TotalSeconds);
        _percentComplete = _elapsed / _duration;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp)
            || args.Viewport.Eye != eyeComp.Eye)
            return false;

        return _percentComplete < 1.0f;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (RequestScreenTexture && ScreenTexture != null)
        {
            // Copied because the screen texture is shared and later overlays overwrite it.
            CaptureFrame(args, ScreenTexture);
            RequestScreenTexture = false;
        }

        var worldHandle = args.WorldHandle;
        _shader.SetParameter("elapsed", _elapsed);
        _shader.SetParameter("duration", _duration);
        _shader.SetParameter("reducedMotion", _reducedMotion);
        worldHandle.UseShader(_shader);
        if (_snapshot != null)
            worldHandle.DrawTextureRectRegion(_snapshot.Texture, args.WorldBounds);
        else
            worldHandle.DrawRect(args.WorldBounds, Color.White);

        worldHandle.UseShader(null);
    }

    private void CaptureFrame(in OverlayDrawArgs args, Texture source)
    {
        _snapshot?.Dispose();
        var snapshot = _clyde.CreateRenderTarget(source.Size,
            new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
            name: "flash-snapshot");
        _snapshot = snapshot;

        var handle = args.RenderHandle.DrawingHandleScreen;
        handle.RenderInRenderTarget(snapshot,
            () =>
            {
                handle.UseShader(null);
                handle.DrawTextureRect(source, UIBox2.FromDimensions(Vector2.Zero, snapshot.Size));
            },
            Color.Black);
    }

    public void Start(bool captureFrame = true)
    {
        Reset();
        _percentComplete = 0f;
        RequestScreenTexture = captureFrame;
    }

    public void Reset()
    {
        _percentComplete = 1f;
        RequestScreenTexture = false;
        _effectEnd = null;
        _elapsed = 0;
        _duration = 1f;
        _snapshot?.Dispose();
        _snapshot = null;
    }

    private void OnReducedMotionChanged(bool enabled)
    {
        _reducedMotion = enabled;
    }

    protected override void DisposeBehavior()
    {
        base.DisposeBehavior();
        _configManager.UnsubValueChanged(CCVars.DisableFlashEffect, OnReducedMotionChanged);
        Reset();
        _shader.Dispose();
    }
}
