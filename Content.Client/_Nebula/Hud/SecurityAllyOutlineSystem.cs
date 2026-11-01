// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Flash;
using Content.Shared._Nebula.CCVar;
using Content.Shared._Nebula.Hud;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.NightVision;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Nebula.Hud;

/// <summary>
/// Outlines the allies from <see cref="SecurityNeuroComponent.Allies"/> in the color of their mob state while night vision with <see cref="AllyOutlineVisionComponent"/> is on.
/// </summary>
public sealed partial class SecurityAllyOutlineSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private EntityQuery<SecurityNeuroComponent> _neuroQuery = default!;
    [Dependency] private EntityQuery<BlindableComponent> _blindableQuery = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery = default!;

    private const string PostShaderId = "nebula-ally-outline";
    private const float Alpha = 0.6f;
    private static readonly ProtoId<ShaderPrototype> Shader = "NebulaAllyOutline";
    private static readonly Color AliveColor = Color.DodgerBlue;
    private static readonly Color CritColor = Color.Yellow;
    private static readonly Color DeadColor = Color.Red;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(0.2);

    /// <summary>
    /// One shader per color, shared by every ally in that state.
    /// </summary>
    private readonly Dictionary<Color, SpriteComponent.PostShaderEntry[]> _shaders = [];

    private SecurityAllyOutlineOverlay _overlay = default!;
    private bool _enabled;
    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new SecurityAllyOutlineOverlay();
        _overlayMan.AddOverlay(_overlay);
        Subs.CVar(_cfg, NCCVars.AllyOutline, OnEnabledChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayMan.RemoveOverlay(_overlay);
    }

    [SubscribeNetworkEvent]
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _overlay.Allies.Clear();
        _nextUpdate = TimeSpan.Zero;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_timing.RealTime < _nextUpdate)
            return;

        _nextUpdate = _timing.RealTime + Interval;
        Refresh();
    }

    private void OnEnabledChanged(bool enabled)
    {
        _enabled = enabled;
        Refresh();
    }

    private void Refresh()
    {
        _overlay.Allies.Clear();
        if (!_enabled
            || _overlayMan.HasOverlay<FlashOverlay>()
            || _player.LocalEntity is not { } local
            || !_neuroQuery.TryComp(local, out var neuro)
            || HasComp<NeuroSignalLostComponent>(local)
            || _blindableQuery.TryComp(local, out var blindable) && blindable.IsBlind
            || !HasOutlineVision(local))
            return;

        foreach (var net in neuro.Allies)
        {
            if (TryGetEntity(net, out var ally))
                _overlay.Allies[ally.Value] = GetShaders(GetColor(ally.Value));
        }
    }

    private bool HasOutlineVision(EntityUid local)
    {
        var ev = new RefreshNightVisionEvent();
        RaiseLocalEvent(local, ref ev);

        foreach (var vision in ev.Entities)
        {
            if (HasComp<AllyOutlineVisionComponent>(vision))
                return true;
        }

        return false;
    }

    private Color GetColor(EntityUid ally)
    {
        if (!_mobStateQuery.TryComp(ally, out var state))
            return AliveColor;

        return state.CurrentState switch
        {
            MobState.SoftCrit or MobState.Critical => CritColor,
            MobState.Dead => DeadColor,
            _ => AliveColor,
        };
    }

    private SpriteComponent.PostShaderEntry[] GetShaders(Color color)
    {
        if (_shaders.TryGetValue(color, out var cached))
            return cached;

        var shader = _proto.Index(Shader).InstanceUnique();
        shader.SetParameter("outline_width", 1f);
        shader.SetParameter("outline_color", color.WithAlpha(Alpha));

        SpriteComponent.PostShaderEntry[] shaders = [new(PostShaderId, shader)];
        _shaders[color] = shaders;
        return shaders;
    }
}
