// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Implants;
using Content.Shared.GameTicking;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.Hud;

/// <summary>
/// Shows the marks and top tasks of security tracking implants whose owners went into crit, each for a while.
/// </summary>
public sealed partial class SecurityImplantMarkSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SkewedHudSystem _hud = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(0.2);
    private static readonly SpriteSpecifier Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/_Nebula/Interface/Squads/sos.svg.192dpi.png"));

    private readonly List<(NetCoordinates Coordinates, SkewedHudTask Task, TimeSpan Until)> _marks = [];
    private readonly List<SkewedHudTask> _tasks = [];
    private Texture? _icon;

    /// <summary>
    /// Some mark lies on a grid outside PVS, its marker is added once the grid arrives.
    /// </summary>
    private bool _unresolved;

    private TimeSpan _nextCheck;

    [SubscribeNetworkEvent]
    private void OnCritMark(SecurityImplantCritMarkEvent ev)
    {
        var text = Loc.GetString("skewed-hud-implant-crit", ("name", ev.Name), ("location", ev.Location));
        _marks.Add((ev.Coordinates, SecurityHud.Task(SecurityHudTaskKind.Implant, text), _timing.RealTime + ev.Duration));
        Rebuild();
    }

    [SubscribeNetworkEvent]
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        Reset();
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        Reset();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var now = _timing.RealTime;
        if (_marks.Count == 0 || now < _nextCheck)
            return;

        _nextCheck = now + CheckInterval;
        if (_marks.RemoveAll(m => m.Until <= now) > 0 || _unresolved)
            Rebuild();
    }

    private void Rebuild()
    {
        _unresolved = false;
        var markers = _hud.MarkersOf(SecurityHud.ImplantSource);
        markers.Clear();
        _tasks.Clear();
        _icon ??= _sprite.Frame0(Icon);
        foreach (var mark in _marks)
        {
            _tasks.Add(mark.Task);
            if (TryGetEntity(mark.Coordinates.NetEntity, out _))
                markers.Add(new SquadMarker(_icon, Color.Red, GetCoordinates(mark.Coordinates)));
            else
                _unresolved = true;
        }

        _hud.SetTasks(SecurityHud.ImplantSource, _tasks);
    }

    private void Reset()
    {
        _marks.Clear();
        _tasks.Clear();
        _unresolved = false;
        _hud.MarkersOf(SecurityHud.ImplantSource).Clear();
        _hud.SetTasks(SecurityHud.ImplantSource, _tasks);
    }
}
