// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared._Nebula.PointTether;
using Content.Shared._Nebula.Silicons.StationAi;
using Content.Shared.Body;
using Content.Shared.GameTicking;
using Content.Shared.Holopad;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Nebula.Silicons.StationAi;

public sealed partial class AiHologramVisualsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private const float InterferenceStart = 3f;
    private static readonly TimeSpan SpriteRefreshInterval = TimeSpan.FromSeconds(0.5);

    private readonly HashSet<EntityUid> _staleDolls = [];

    /// <summary>
    /// Entity whose sprite the hologram of <paramref name="target"/> copies.
    /// </summary>
    public EntityUid? GetAppearanceSource(EntityUid? target)
    {
        return TryComp<AiHologramProfileComponent>(target, out var comp) && comp.Doll is { } doll
            ? doll
            : target;
    }

    /// <summary>
    /// Copying the source's layers is too expensive for every frame, so it happens when the source changes
    /// and periodically to pick up changes in its appearance.
    /// </summary>
    public bool NeedsSpriteRebuild(Entity<HolopadHologramComponent> hologram)
    {
        var source = GetAppearanceSource(hologram.Comp.LinkedEntity);
        var now = _timing.RealTime;
        if (source == hologram.Comp.SpriteSource && now < hologram.Comp.NextSpriteRefresh)
            return false;

        hologram.Comp.SpriteSource = source;
        hologram.Comp.NextSpriteRefresh = now + SpriteRefreshInterval;
        return true;
    }

    public bool IsWalking(EntityUid hologram)
    {
        return HasComp<PointTetherComponent>(Transform(hologram).ParentUid);
    }

    /// <summary>
    /// Signal loss of a walking hologram: 0 is clean, 1 is fully dissolved.
    /// </summary>
    public float GetInterference(EntityUid hologram)
    {
        var parent = Transform(hologram).ParentUid;
        if (!TryComp<PointTetherComponent>(parent, out var tether) || !Exists(tether.Anchor))
            return 0f;

        var distance = (_transform.GetWorldPosition(parent) - _transform.GetWorldPosition(tether.Anchor)).Length();
        return Math.Clamp((distance - InterferenceStart) / (tether.Range - InterferenceStart), 0f, 1f);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_staleDolls.Count == 0)
            return;

        // Built here because states are handled before rendering, and spawning mid-render is unsafe.
        foreach (var uid in _staleDolls)
        {
            if (TryComp<AiHologramProfileComponent>(uid, out var comp))
                RebuildDoll(comp);
        }

        _staleDolls.Clear();
    }

    private void RebuildDoll(AiHologramProfileComponent comp)
    {
        QueueDel(comp.Doll);
        comp.Doll = null;

        if (comp.Profile is not { } profile || !_proto.TryIndex(profile.Species, out var species))
            return;

        var doll = Spawn(species.DollPrototype, MapCoordinates.Nullspace);
        _visualBody.ApplyProfileTo(doll, profile);
        _sprite.SetScale(doll, new Vector2(profile.Width, profile.Height));
        comp.Doll = doll;
    }

    [SubscribeLocalEvent]
    private void OnProfileState(Entity<AiHologramProfileComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _staleDolls.Add(ent);
    }

    [SubscribeLocalEvent]
    private void OnProfileShutdown(Entity<AiHologramProfileComponent> ent, ref ComponentShutdown args)
    {
        _staleDolls.Remove(ent);
        QueueDel(ent.Comp.Doll);
    }

    [SubscribeLocalEvent]
    private void OnRoundCleanup(RoundRestartCleanupEvent args)
    {
        _staleDolls.Clear();
    }
}
