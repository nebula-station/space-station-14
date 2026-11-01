// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Shared._Nebula.Hud;
using Content.Shared._Nebula.Squads;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Nebula.Hud;

/// <summary>
/// Security module of the neural interface: feeds the HUD from the local player's <see cref="SecurityNeuroComponent"/>.
/// The squad, its orders and the tasks come from the server, the orders are also markers on the screen.
/// New orders and head of security tasks beep.
/// </summary>
public sealed partial class SecurityNeuroSystem : EntitySystem
{
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SkewedHudSystem _hud = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private AudioSystem _audio = default!;

    [Dependency] private EntityQuery<SecurityNeuroComponent> _neuroQuery = default!;

    private static readonly TimeSpan AlertTime = TimeSpan.FromSeconds(10);
    private static readonly SoundSpecifier NewOrderSound = new SoundPathSpecifier("/Audio/_Nebula/Effects/tablet_order.ogg");

    /// <summary>
    /// Short lived alert lines of the commander: losses and brig timers that ran out.
    /// </summary>
    private readonly List<(SecurityHudTaskKind Kind, string Text, TimeSpan Until)> _alerts = [];

    private readonly HashSet<int> _knownMarks = [];
    private readonly HashSet<int> _knownOrders = [];
    private readonly Dictionary<ProtoId<SquadOrderPrototype>, Texture> _orderIcons = [];

    /// <summary>
    /// False until the first state after attaching, so the tasks the player spawns with do not beep.
    /// </summary>
    private bool _seeded;

    /// <summary>
    /// Some mark lies on a grid outside PVS, its marker is added once the grid arrives.
    /// </summary>
    private bool _unresolvedMarks;

    [SubscribeNetworkEvent]
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _alerts.Clear();
        _knownMarks.Clear();
        _knownOrders.Clear();
        _orderIcons.Clear();
        _seeded = false;
        _unresolvedMarks = false;
    }

    #region Component events

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<SecurityNeuroComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Owner != _player.LocalEntity)
            return;

        Update(ent.Comp);
    }

    [SubscribeLocalEvent(after: [typeof(NeuroLinkSystem)])]
    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        _alerts.Clear();
        _seeded = false;
        if (_neuroQuery.TryComp(args.Entity, out var neuro))
            Update(neuro);
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        _window?.Close();
    }

    [SubscribeNetworkEvent]
    private void OnLoss(SecurityNeuroLossEvent ev)
    {
        var key = ev.State == MobState.Dead ? "skewed-hud-loss-dead" : "skewed-hud-loss-crit";
        AddAlert(SecurityHudTaskKind.Loss, Loc.GetString(key, ("name", ev.Name), ("squad", ev.Squad)));
    }

    [SubscribeNetworkEvent]
    private void OnTimer(SecurityNeuroTimerEvent ev)
    {
        AddAlert(SecurityHudTaskKind.Timer, Loc.GetString("skewed-hud-timer-done", ("name", ev.Name)));
        _audio.PlayGlobal(NewOrderSound, Filter.Local(), false);
    }

    #endregion

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_alerts.Count > 0 && _alerts.RemoveAll(l => l.Until <= _timing.RealTime) > 0)
            UpdateAlerts();

        CloseInactiveWindow();

        if (_unresolvedMarks
            && _player.LocalEntity is { } uid
            && _neuroQuery.TryComp(uid, out var neuro))
            UpdateMarkers(neuro);
    }

    /// <summary>
    /// Pushes the local player's squad and tasks to the HUD again, after something else drew on it.
    /// </summary>
    public void Resync()
    {
        if (_player.LocalEntity is { } uid && _neuroQuery.TryComp(uid, out var neuro))
            Update(neuro);
    }

    #region Helpers

    private void Update(SecurityNeuroComponent neuro)
    {
        // A member sees their squad, the head of security has none and sees all squads instead.
        List<SkewedHudLine> squad = [];
        foreach (var member in neuro.Squad)
        {
            squad.Add(SecurityHud.Member(member.Name, member.State));
        }

        if (squad.Count == 0)
        {
            foreach (var line in neuro.Summary)
            {
                squad.Add(SecurityHud.Summary(line.Name, line.Color, line.Alive, line.Total, line.Losses));
            }
        }

        _hud.SetSection(SecurityHud.Source, SecurityHud.SquadSection, 0, squad);

        List<SkewedHudTask> tasks = [];
        foreach (var mark in neuro.Marks)
        {
            if (!_proto.Resolve(mark.Order, out var order))
                continue;

            var text = Loc.GetString("skewed-hud-mark", ("order", Loc.GetString(order.Name)), ("location", mark.Location));
            tasks.Add(SecurityHud.Task(SecurityHudTaskKind.Mark, text));
        }

        foreach (var task in neuro.Tasks)
        {
            if (!task.Pinned)
                continue;

            var kind = task.Key.Kind switch
            {
                SecurityNeuroTaskKind.Order => SecurityHudTaskKind.Order,
                SecurityNeuroTaskKind.Personal => SecurityHudTaskKind.Personal,
                _ => SecurityHudTaskKind.Wanted,
            };

            // The description is not shown on the HUD, a task with one gets a mark after its title.
            var text = task.Description.Length == 0
                ? task.Title
                : Loc.GetString("skewed-hud-task-described", ("title", task.Title));
            tasks.Add(SecurityHud.Task(kind, text));
        }

        _hud.SetTasks(SecurityHud.Source, tasks);
        UpdateMarkers(neuro);
        BeepOnNew(neuro);
        UpdateWindow(neuro);
    }

    private void UpdateMarkers(SecurityNeuroComponent neuro)
    {
        var markers = _hud.MarkersOf(SecurityHud.Source);
        markers.Clear();
        _unresolvedMarks = false;
        foreach (var mark in neuro.Marks)
        {
            if (!TryGetEntity(mark.Coordinates.NetEntity, out _))
            {
                _unresolvedMarks = true;
                continue;
            }

            if (!TryGetOrderIcon(mark.Order, out var icon))
                continue;

            markers.Add(new SquadMarker(icon, mark.Color, GetCoordinates(mark.Coordinates)));
        }
    }

    private bool TryGetOrderIcon(ProtoId<SquadOrderPrototype> id, [NotNullWhen(true)] out Texture? icon)
    {
        if (_orderIcons.TryGetValue(id, out icon))
            return true;

        if (!_proto.Resolve(id, out var order))
            return false;

        icon = _sprite.Frame0(order.Icon);
        _orderIcons[id] = icon;
        return true;
    }

    private void BeepOnNew(SecurityNeuroComponent neuro)
    {
        var added = false;
        foreach (var mark in neuro.Marks)
        {
            added |= _knownMarks.Add(mark.Id);
        }

        foreach (var task in neuro.Tasks)
        {
            if (task.Key.Kind == SecurityNeuroTaskKind.Order)
                added |= _knownOrders.Add(task.Key.Id);
        }

        if (added && _seeded)
            _audio.PlayGlobal(NewOrderSound, Filter.Local(), false);

        _seeded = true;
    }

    private void AddAlert(SecurityHudTaskKind kind, string text)
    {
        _alerts.Add((kind, text, _timing.RealTime + AlertTime));
        UpdateAlerts();
    }

    private void UpdateAlerts()
    {
        List<SkewedHudTask> alerts = new(_alerts.Count);
        foreach (var alert in _alerts)
        {
            alerts.Add(SecurityHud.Task(alert.Kind, alert.Text));
        }

        _hud.SetTasks(SecurityHud.AlertSource, alerts);
    }

    #endregion
}
