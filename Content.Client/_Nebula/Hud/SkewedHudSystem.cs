// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.GameTicking;
using Content.Shared._Nebula.Hud;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._Nebula.Hud;

/// <summary>
/// Holds the state of the neural interface HUD and owns its overlay. It knows nothing of the roles:
/// <see cref="NeuroLinkSystem"/> fills the common block (location, status, code), the module of the profile fills
/// its own sections of lines, the tasks and the markers, all under the module's source name.
/// Every setter stamps the change time, so the overlay can play the decrypt animation on the changed line only.
/// </summary>
public sealed partial class SkewedHudSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ClientGameTicker _ticker = default!;

    public const int MaxVisibleTasks = 5;

    /// <summary>
    /// Place of the location, status and code block among the module sections.
    /// </summary>
    public const int InfoOrder = 100;

    /// <summary>
    /// Colors of the HUD lines by their state.
    /// </summary>
    public static readonly (MobState State, Color Color)[] StateColors =
    [
        (MobState.Alive, Color.FromHex("#4a8cff")),
        (MobState.SoftCrit, Color.FromHex("#ffd23f")),
        (MobState.Critical, Color.FromHex("#ffd23f")),
        (MobState.Dead, Color.FromHex("#ff4040")),
    ];

    private bool _active;

    /// <summary>
    /// Turning the HUD on stamps <see cref="ActivatedAt"/>, which drives the boot animation.
    /// </summary>
    public bool Active
    {
        get => _active;
        set
        {
            if (value && !_active)
                ActivatedAt = _timing.RealTime;

            _active = value;
        }
    }

    public TimeSpan ActivatedAt { get; private set; }

    /// <summary>
    /// Look of the HUD, the default colors when null.
    /// </summary>
    public NeuroProfilePrototype? Profile { get; private set; }

    /// <summary>
    /// Set when the owner dies with the HUD on: the rows break up bottom to top and the HUD fades out.
    /// </summary>
    public TimeSpan? FlatlineAt { get; private set; }

    /// <summary>
    /// Set while the interface has no link to the station servers: the squad, location and code rows lag
    /// and a banner says the link is lost.
    /// </summary>
    public TimeSpan? SignalLostAt { get; private set; }

    /// <summary>
    /// When the link came back, the banner fades out after it.
    /// </summary>
    public TimeSpan SignalRestoredAt { get; private set; }

    /// <summary>
    /// Bumped on every data change, so the overlay rebuilds its rows only when something changed.
    /// </summary>
    public int Version { get; private set; }

    public TimeSpan ShiftTime => _timing.CurTime - _ticker.RoundStartTimeSpan;

    /// <summary>
    /// Sections of the modules by <see cref="SkewedHudSection.Order"/>.
    /// </summary>
    public readonly List<SkewedHudSection> Sections = [];
    public readonly List<SkewedHudTask> Tasks = [];

    /// <summary>
    /// Spots on the screen by source, hidden while the link is lost.
    /// </summary>
    public readonly Dictionary<string, List<SquadMarker>> Markers = [];

    public string? Location { get; private set; }
    public TimeSpan LocationChangedAt { get; private set; }
    public MobState SelfState { get; private set; } = MobState.Alive;
    public TimeSpan SelfStateChangedAt { get; private set; }
    public string? AlertName { get; private set; }
    public Color AlertColor { get; private set; } = Color.White;
    public TimeSpan AlertChangedAt { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        _overlay.AddOverlay(new SkewedHudOverlay(this, _player, _eye, _ui, _input, _cache, _timing, _random));
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<SkewedHudOverlay>();
    }

    [SubscribeNetworkEvent]
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        Active = false;
        Clear();
    }

    /// <summary>
    /// Puts the lines of a module section on the HUD, or takes the section off when there are none.
    /// A line that stays the same at its place keeps its change time, so only the changed lines animate.
    /// </summary>
    public void SetSection(string source, string id, int order, List<SkewedHudLine> lines, bool right = false)
    {
        var index = Sections.FindIndex(s => s.Source == source && s.Id == id);
        if (lines.Count == 0)
        {
            if (index < 0)
                return;

            Sections.RemoveAt(index);
            Version++;
            return;
        }

        var section = index >= 0
            ? Sections[index]
            : new SkewedHudSection { Source = source, Id = id, Order = order };
        var changed = index < 0 || section.Order != order || section.Right != right || section.Lines.Count != lines.Count;
        for (var i = 0; i < lines.Count; i++)
        {
            if (i < section.Lines.Count && SameLine(section.Lines[i], lines[i]))
            {
                lines[i] = lines[i] with { ChangedAt = section.Lines[i].ChangedAt };
                continue;
            }

            lines[i] = lines[i] with { ChangedAt = _timing.RealTime };
            changed = true;
        }

        if (!changed)
            return;

        section.Order = order;
        section.Right = right;
        section.Lines.Clear();
        section.Lines.AddRange(lines);
        if (index < 0)
        {
            var at = Sections.FindIndex(s => s.Order > order);
            Sections.Insert(at < 0 ? Sections.Count : at, section);
        }
        else
        {
            Sections.Sort((x, y) => x.Order.CompareTo(y.Order));
        }

        Version++;
    }

    private static bool SameLine(SkewedHudLine a, SkewedHudLine b)
    {
        return a.Label == b.Label
            && a.Value == b.Value
            && a.Color == b.Color
            && a.LabelColor == b.LabelColor
            && a.Pulse == b.Pulse
            && a.Lags == b.Lags;
    }

    /// <summary>
    /// Color of a state on the HUD lines.
    /// </summary>
    public static Color StateColor(MobState state)
    {
        foreach (var (s, color) in StateColors)
        {
            if (s == state)
                return color;
        }

        return StateColors[0].Color;
    }

    /// <summary>
    /// Location and alert names are shown in capitals, like the rest of the visor's key values.
    /// </summary>
    public void SetLocation(string? location)
    {
        location = location?.ToUpperInvariant();
        if (Location == location)
            return;

        Location = location;
        LocationChangedAt = _timing.RealTime;
        Version++;
    }

    public void SetSelfState(MobState state)
    {
        if (SelfState == state)
            return;

        SelfState = state;
        SelfStateChangedAt = _timing.RealTime;
        Version++;
    }

    public void SetAlert(string? name, Color color)
    {
        name = name?.ToUpperInvariant();
        if (AlertName == name)
        {
            if (AlertColor != color)
            {
                AlertColor = color;
                Version++;
            }

            return;
        }

        AlertName = name;
        AlertColor = color;
        AlertChangedAt = _timing.RealTime;
        Version++;
    }

    public void SetProfile(NeuroProfilePrototype? profile)
    {
        if (Profile == profile)
            return;

        Profile = profile;
        Version++;
    }

    /// <summary>
    /// Inserts the task after all tasks of the same or lower order.
    /// </summary>
    public void AddTask(SkewedHudTask task)
    {
        var index = Tasks.FindIndex(t => t.Order > task.Order);
        Tasks.Insert(index < 0 ? Tasks.Count : index, task with { ChangedAt = _timing.RealTime });
        Version++;
    }

    /// <summary>
    /// Replaces all tasks of the source with <paramref name="tasks"/>.
    /// Tasks that stay keep their change time, so only new lines animate.
    /// </summary>
    public void SetTasks(string source, List<SkewedHudTask> tasks)
    {
        var removed = Tasks.RemoveAll(t => t.Source == source && !tasks.Exists(n => SameTask(t, n)));
        if (removed > 0)
            Version++;

        foreach (var task in tasks)
        {
            if (!Tasks.Exists(t => t.Source == source && SameTask(t, task)))
                AddTask(task with { Source = source });
        }
    }

    private static bool SameTask(SkewedHudTask a, SkewedHudTask b)
    {
        return a.Label == b.Label
            && a.Text == b.Text
            && a.Order == b.Order
            && a.Urgent == b.Urgent;
    }

    /// <summary>
    /// The marker list of the source, for the module to clear and fill.
    /// </summary>
    public List<SquadMarker> MarkersOf(string source)
    {
        if (!Markers.TryGetValue(source, out var markers))
        {
            markers = [];
            Markers[source] = markers;
        }

        return markers;
    }

    public void RemoveTask(int index)
    {
        Tasks.RemoveAt(index);
        Version++;
    }

    /// <summary>
    /// Coming back from the dead plays the boot animation again.
    /// </summary>
    public void SetFlatline(bool dead)
    {
        if (dead == (FlatlineAt != null))
            return;

        FlatlineAt = dead ? _timing.RealTime : null;
        if (!dead)
            ActivatedAt = _timing.RealTime;

        Version++;
    }

    public void SetSignalLost(bool lost)
    {
        if (lost == (SignalLostAt != null))
            return;

        SignalLostAt = lost ? _timing.RealTime : null;
        if (!lost)
            SignalRestoredAt = _timing.RealTime;
    }

    /// <summary>
    /// Drops all data but keeps the HUD on, since <see cref="Active"/> belongs to whoever turned it on.
    /// </summary>
    public void Clear()
    {
        Sections.Clear();
        Tasks.Clear();
        foreach (var markers in Markers.Values)
        {
            markers.Clear();
        }

        FlatlineAt = null;
        SignalLostAt = null;
        SignalRestoredAt = TimeSpan.Zero;
        Location = null;
        SelfState = MobState.Alive;
        AlertName = null;
        AlertColor = Color.White;
        Version++;
    }
}
