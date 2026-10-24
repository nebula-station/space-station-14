// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Nebula.Manifest;
using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Client._Nebula.UserInterface;
using Content.Client.CrewManifest;
using Content.Client.GameTicking;
using Content.Client.Lobby;
using Content.Client.Players.PlayTimeTracking;
using Content.Shared.CCVar;
using Content.Shared.Roles;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.LateJoin;

/// <summary>
/// Late join job picker in the Nebula style: one page per station with jobs grouped by department.
/// </summary>
public sealed partial class NebulaLateJoinWindow : NebulaWindow
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IClientConsoleHost _console = default!;
    [Dependency] private IEntitySystemManager _entitySystems = default!;
    [Dependency] private JobRequirementsManager _jobRequirements = default!;
    [Dependency] private IClientPreferencesManager _preferences = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private readonly ClientGameTicker _gameTicker;
    private readonly CrewManifestSystem _crewManifest;

    private readonly BoxContainer _stations;
    private readonly BoxContainer _pages;
    private readonly Dictionary<NetEntity, Dictionary<ProtoId<JobPrototype>, List<JobRow>>> _rows = new();
    private readonly Dictionary<NetEntity, ShapedButton> _tabs = new();
    private readonly Dictionary<NetEntity, Control> _stationPages = new();

    private NetEntity? _selectedStation;

    public NebulaLateJoinWindow()
    {
        IoCManager.InjectDependencies(this);

        _gameTicker = _entitySystems.GetEntitySystem<ClientGameTicker>();
        _crewManifest = _entitySystems.GetEntitySystem<CrewManifestSystem>();

        Title = Loc.GetString("late-join-gui-title");
        SetSize = new Vector2(420, 640);
        MinSize = new Vector2(340, 360);

        _stations = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
        };

        _pages = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
        };

        Contents.AddChild(_stations);
        Contents.AddChild(_pages);
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        _jobRequirements.Updated += Rebuild;
        _gameTicker.LobbyJobsAvailableUpdated += OnJobsAvailableUpdated;
        Rebuild();
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        _jobRequirements.Updated -= Rebuild;
        _gameTicker.LobbyJobsAvailableUpdated -= OnJobsAvailableUpdated;
    }

    private void Rebuild()
    {
        _stations.RemoveAllChildren();
        _pages.RemoveAllChildren();
        _rows.Clear();
        _tabs.Clear();
        _stationPages.Clear();

        if (_selectedStation == null || !_gameTicker.StationNames.ContainsKey(_selectedStation.Value))
            _selectedStation = null;

        foreach (var (station, name) in _gameTicker.StationNames)
        {
            _selectedStation ??= station;

            var tab = new ShapedButton { Text = name, ToggleMode = true };
            tab.AddStyleClass(NebulaStyleClass.ButtonTonal);
            tab.OnPressed += _ => SelectStation(station);
            _stations.AddChild(tab);
            _tabs[station] = tab;

            var page = CreatePage(station);
            _pages.AddChild(page);
            _stationPages[station] = page;
        }

        _stations.Visible = _stations.ChildCount > 1;

        if (_pages.ChildCount == 0)
        {
            var empty = new Label
            {
                Text = Loc.GetString("nebula-late-join-no-stations"),
                Align = Label.AlignMode.Center,
                VerticalExpand = true,
            };
            empty.AddStyleClass(NebulaStyleClass.TextMuted);
            _pages.AddChild(empty);
        }

        UpdatePages();
    }

    private void SelectStation(NetEntity station)
    {
        _selectedStation = station;
        UpdatePages();
    }

    private void UpdatePages()
    {
        foreach (var (station, tab) in _tabs)
        {
            tab.Pressed = station == _selectedStation;
        }

        foreach (var (station, page) in _stationPages)
        {
            page.Visible = station == _selectedStation;
        }
    }

    private Control CreatePage(NetEntity station)
    {
        var page = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
            SeparationOverride = 8,
        };

        if (_cfg.GetCVar(CCVars.CrewManifestWithoutEntity))
        {
            var manifest = new ShapedButton
            {
                Text = Loc.GetString("crew-manifest-button-label"),
                Icon = NebulaIcons.Get("assignment"),
                HorizontalAlignment = HAlignment.Left,
            };
            manifest.AddStyleClass(NebulaStyleClass.ButtonTonal);
            manifest.OnPressed += _ => _crewManifest.RequestCrewManifest(station);
            page.AddChild(manifest);
        }

        var listing = new NebulaManifestListing();
        page.AddChild(new ScrollContainer
        {
            VerticalExpand = true,
            HScrollEnabled = false,
            Children = { listing },
        });

        var rows = _rows[station] = new Dictionary<ProtoId<JobPrototype>, List<JobRow>>();

        if (!_gameTicker.JobsAvailable.TryGetValue(station, out var available))
            return page;

        JobUIComparer.TryCreate(_prototypes, _gameTicker.JobWeightsByStation.GetValueOrDefault(station), out var comparer);

        var departments = new List<DepartmentPrototype>(_prototypes.EnumeratePrototypes<DepartmentPrototype>());
        departments.Sort(DepartmentUIComparer.Instance);

        foreach (var department in departments)
        {
            var jobs = new List<JobPrototype>();

            foreach (var jobId in department.Roles)
            {
                if (available.ContainsKey(jobId) && _prototypes.TryIndex(jobId, out var job))
                    jobs.Add(job);
            }

            if (jobs.Count == 0)
                continue;

            if (comparer != null)
                jobs.Sort(comparer);

            var section = listing.AddDepartment(department);

            foreach (var job in jobs)
            {
                var row = CreateJobRow(listing, station, job, available[job.ID]);
                section.AddChild(row.Button);

                // Heads are listed under Command and their own department, both rows need slot updates.
                if (!rows.TryGetValue(job.ID, out var jobRows))
                    rows[job.ID] = jobRows = new List<JobRow>();

                jobRows.Add(row);
            }
        }

        if (rows.Count == 0)
            listing.AddMessage(Loc.GetString("nebula-late-join-no-jobs"));

        return page;
    }

    private JobRow CreateJobRow(NebulaManifestListing listing, NetEntity station, JobPrototype job, int? slots)
    {
        var button = new ShapedButton { HorizontalExpand = true };
        button.AddStyleClass(NebulaStyleClass.ButtonListItem);
        button.Content.HorizontalAlignment = HAlignment.Stretch;
        button.Content.Margin = new Thickness(8, 0);

        if (_prototypes.TryIndex(job.Icon, out var icon))
            button.Content.AddChild(listing.CreateJobIcon(icon));

        var name = new Label { Text = job.LocalizedName, HorizontalExpand = true, VerticalAlignment = VAlignment.Center };
        name.AddStyleClass(NebulaStyleClass.TextValue);
        button.Content.AddChild(name);

        var slotsLabel = new Label { VerticalAlignment = VAlignment.Center };
        slotsLabel.AddStyleClass(NebulaStyleClass.TextMuted);
        button.Content.AddChild(slotsLabel);

        var row = new JobRow(button, slotsLabel);

        if (!_jobRequirements.IsAllowed(job, _preferences.Preferences?.SelectedCharacter, out var reason))
        {
            row.Locked = true;

            button.Content.AddChild(new TextureRect
            {
                Texture = NebulaIcons.Get("lock"),
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
                SetSize = new Vector2(14, 14),
                VerticalAlignment = VAlignment.Center,
                StyleClasses = { NebulaStyleClass.Icon },
            });

            if (!reason.IsEmpty)
            {
                var tooltip = new Tooltip();
                tooltip.SetMessage(reason);
                button.TooltipSupplier = _ => tooltip;
            }
        }

        row.SetSlots(slots);
        button.OnPressed += _ => Join(station, job.ID);
        return row;
    }

    private void Join(NetEntity station, ProtoId<JobPrototype> job)
    {
        _console.ExecuteCommand($"joingame {CommandParsing.Escape(job)} {station}");
        Close();
    }

    private void OnJobsAvailableUpdated(IReadOnlyDictionary<NetEntity, Dictionary<ProtoId<JobPrototype>, int?>> jobs)
    {
        foreach (var (station, available) in jobs)
        {
            if (!_rows.TryGetValue(station, out var rows))
                continue;

            foreach (var (job, jobRows) in rows)
            {
                if (!available.TryGetValue(job, out var slots))
                    continue;

                foreach (var row in jobRows)
                {
                    row.SetSlots(slots);
                }
            }
        }
    }

    private sealed class JobRow(ShapedButton button, Label slots)
    {
        private const float DisabledAlpha = 0.45f;

        public readonly ShapedButton Button = button;
        public bool Locked;

        public void SetSlots(int? amount)
        {
            slots.Text = amount?.ToString() ?? "∞";
            Button.Disabled = Locked || amount == 0;
            Button.Content.Modulate = Button.Disabled ? Color.White.WithAlpha(DisabledAlpha) : Color.White;
        }
    }
}
