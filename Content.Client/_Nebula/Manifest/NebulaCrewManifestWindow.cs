// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Shared.CrewManifest;
using Content.Shared.Roles;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._Nebula.Manifest;

public sealed partial class NebulaCrewManifestWindow : NebulaWindow
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    private readonly Label _station;
    private readonly NebulaManifestListing _listing = new();

    public NebulaCrewManifestWindow()
    {
        IoCManager.InjectDependencies(this);

        Title = Loc.GetString("crew-manifest-window-title");
        SetSize = new Vector2(460, 640);
        MinSize = new Vector2(340, 300);

        _station = new Label { Margin = new Thickness(6, 0) };
        _station.AddStyleClass(NebulaStyleClass.TextMuted);

        Contents.AddChild(_station);
        Contents.AddChild(new ScrollContainer
        {
            VerticalExpand = true,
            HScrollEnabled = false,
            Children = { _listing },
        });
    }

    public void Populate(string station, CrewManifestEntries? entries)
    {
        _listing.Clear();
        _station.Text = station;
        _station.Visible = entries != null;

        if (entries == null)
        {
            _listing.AddMessage(Loc.GetString("crew-manifest-no-valid-station"));
            return;
        }

        var departments = _prototypes.EnumeratePrototypes<DepartmentPrototype>().ToList();
        departments.Sort(DepartmentUIComparer.Instance);

        foreach (var department in departments)
        {
            var crew = entries.Entries
                .Where(entry => department.Roles.Contains(entry.JobPrototype))
                .ToList();

            if (crew.Count == 0)
                continue;

            var section = _listing.AddDepartment(department, crew.Count.ToString());

            foreach (var entry in crew)
            {
                _listing.AddPerson(section, entry.Name, entry.JobTitle, entry.JobIcon);
            }
        }
    }
}
