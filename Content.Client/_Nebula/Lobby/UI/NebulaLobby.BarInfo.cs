// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface;
using Content.Client.Lobby;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._Nebula.Lobby.UI;

public sealed partial class NebulaLobby
{
    [Dependency] private IClientPreferencesManager _preferences = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private NebulaThemeManager _theme = default!;

    private const int PopupJobsPerPriority = 6;

    private static readonly JobPriority[] ShownPriorities = [JobPriority.High, JobPriority.Medium, JobPriority.Low];

    private SharedJobSystem? _jobs;
    private PlayerPreferences? _shownPreferences;

    private void InitBarInfo()
    {
        PlayersIcon.Texture = NebulaIcons.Get("group");
        RoleIcon.AddStyleClass(NebulaStyleClass.BarIcon);

        NebulaBarPopup.Attach(PlayersGroup, BuildPlayersPopup);
        NebulaBarPopup.Attach(RoleGroup, BuildRolePopup);
    }

    private void UpdatePlayers()
    {
        if (_gameTicker is not { } ticker)
            return;

        var started = ticker.IsGameStarted;

        PlayersLabel.Text = ticker.LobbyPlayerCount.ToString();
        PlayersStatusIcon.Texture = NebulaIcons.Get(started ? "rocket_launch" : "check_circle");
        PlayersStatusLabel.Text = (started ? ticker.LobbyInGameCount : ticker.LobbyReadyCount).ToString();
    }

    private NebulaBarPopup BuildPlayersPopup()
    {
        var popup = new NebulaBarPopup();
        popup.AddHeader(Loc.GetString("nebula-lobby-players-popup-title"));

        if (_gameTicker is not { } ticker)
            return popup;

        var online = ticker.LobbyPlayerCount;
        var inGame = ticker.LobbyInGameCount;

        popup.AddRow(NebulaIcons.Get("group"), Loc.GetString("nebula-lobby-players-online"), online.ToString());
        popup.AddRow(NebulaIcons.Get("hourglass_top"), Loc.GetString("nebula-lobby-players-in-lobby"), Math.Max(0, online - inGame).ToString());

        if (ticker.IsGameStarted)
            popup.AddRow(NebulaIcons.Get("rocket_launch"), Loc.GetString("nebula-lobby-players-in-game"), inGame.ToString());
        else
            popup.AddRow(NebulaIcons.Get("check_circle"), Loc.GetString("nebula-lobby-players-ready"), ticker.LobbyReadyCount.ToString());

        return popup;
    }

    private void UpdateRoleIfChanged()
    {
        if (!_preferences.ServerDataLoaded)
        {
            RoleGroup.Visible = false;
            return;
        }

        var preferences = _preferences.Preferences;

        // Preferences are replaced on every change, comparing references is enough.
        if (ReferenceEquals(preferences, _shownPreferences))
            return;

        _shownPreferences = preferences;
        RoleGroup.Visible = true;

        if (GetSelectedProfile() is not { } profile || !TryGetTopJob(profile, out var job))
        {
            RoleIcon.Texture = GetDepartmentIcon(null);
            RoleIcon.ModulateSelfOverride = null;
            RoleLabel.Text = Loc.GetString("nebula-lobby-role-none");
            return;
        }

        var department = GetDepartment(job);
        RoleIcon.Texture = GetDepartmentIcon(department);
        RoleIcon.ModulateSelfOverride = GetDepartmentColor(department);
        RoleLabel.Text = job.LocalizedName;
    }

    private NebulaBarPopup BuildRolePopup()
    {
        var popup = new NebulaBarPopup();
        popup.AddHeader(Loc.GetString("nebula-lobby-role-popup-title"));

        if (GetSelectedProfile() is not { } profile)
        {
            popup.AddRow(null, Loc.GetString("nebula-lobby-role-none-hint"), muted: true);
            return popup;
        }

        var any = false;

        foreach (var priority in ShownPriorities)
        {
            var jobs = GetJobs(profile, priority);

            if (jobs.Count == 0)
                continue;

            any = true;
            popup.AddRow(null, Loc.GetString($"nebula-lobby-role-priority-{priority.ToString().ToLowerInvariant()}"), muted: true);

            foreach (var job in jobs.Take(PopupJobsPerPriority))
            {
                var department = GetDepartment(job);
                popup.AddRow(GetDepartmentIcon(department), job.LocalizedName, iconColor: GetDepartmentColor(department));
            }

            if (jobs.Count > PopupJobsPerPriority)
                popup.AddRow(null, Loc.GetString("nebula-lobby-role-more", ("count", jobs.Count - PopupJobsPerPriority)), muted: true);
        }

        if (!any)
            popup.AddRow(null, Loc.GetString("nebula-lobby-role-none-hint"), muted: true);

        return popup;
    }

    private HumanoidCharacterProfile? GetSelectedProfile()
    {
        if (!_preferences.ServerDataLoaded || _preferences.Preferences is not { } preferences)
            return null;

        return preferences.Characters.GetValueOrDefault(preferences.SelectedCharacterIndex);
    }

    private bool TryGetTopJob(HumanoidCharacterProfile profile, [NotNullWhen(true)] out JobPrototype? job)
    {
        foreach (var priority in ShownPriorities)
        {
            job = GetJobs(profile, priority).FirstOrDefault();

            if (job != null)
                return true;
        }

        job = null;
        return false;
    }

    private List<JobPrototype> GetJobs(HumanoidCharacterProfile profile, JobPriority priority)
    {
        var jobs = new List<JobPrototype>();

        foreach (var (id, jobPriority) in profile.JobPriorities)
        {
            if (jobPriority == priority && _prototypes.TryIndex(id, out var job))
                jobs.Add(job);
        }

        jobs.Sort((a, b) => string.Compare(a.LocalizedName, b.LocalizedName, StringComparison.CurrentCulture));
        return jobs;
    }

    private DepartmentPrototype? GetDepartment(JobPrototype job)
    {
        return _jobs != null && _jobs.TryGetPrimaryDepartment(job.ID, out var department) ? department : null;
    }

    private Texture GetDepartmentIcon(DepartmentPrototype? department)
    {
        return NebulaDepartments.GetIcon(department);
    }

    private Color? GetDepartmentColor(DepartmentPrototype? department)
    {
        return NebulaDepartments.GetColor(_theme.Palette, department);
    }
}
