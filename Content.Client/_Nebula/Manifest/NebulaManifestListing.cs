// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Nebula.Lobby;
using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._Nebula.Manifest;

/// <summary>
/// Manifest laid out as one section per department with job and person rows.
/// </summary>
public sealed partial class NebulaManifestListing : BoxContainer
{
    [Dependency] private IEntitySystemManager _entitySystems = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private NebulaThemeManager _theme = default!;

    private const float IconSize = 16f;
    private const float PersonIndent = 24f;

    public NebulaManifestListing()
    {
        IoCManager.InjectDependencies(this);

        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        SeparationOverride = 8;
    }

    public void Clear()
    {
        RemoveAllChildren();
    }

    public BoxContainer AddDepartment(DepartmentPrototype department, string? count = null)
    {
        return AddSection(NebulaDepartments.GetIcon(department),
            NebulaDepartments.GetColor(_theme.Palette, department),
            Loc.GetString(department.Name),
            count);
    }

    /// <summary>
    /// Returns the container the section's rows go into.
    /// </summary>
    /// <param name="accentClass">Accent style class like <see cref="NebulaStyleClass.AccentError"/> for the icon and title.</param>
    public BoxContainer AddSection(Texture icon, Color? iconColor, string title, string? count = null, string? accentClass = null)
    {
        var section = new ShapedPanel { Orientation = LayoutOrientation.Vertical };
        section.AddStyleClass(NebulaStyleClass.Section);

        var body = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Margin = new Thickness(12, 8, 12, 10),
            SeparationOverride = 2,
        };

        var header = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            Margin = new Thickness(0, 0, 0, 4),
        };

        var iconRect = CreateIcon(icon, iconColor);
        header.AddChild(iconRect);

        var name = new Label { Text = title, HorizontalExpand = true };
        name.AddStyleClass(NebulaStyleClass.Title);
        header.AddChild(name);

        if (accentClass != null)
        {
            iconRect.AddStyleClass(accentClass);
            name.AddStyleClass(accentClass);
        }

        if (count != null)
            header.AddChild(CreateLabel(count, NebulaStyleClass.TextMuted));

        body.AddChild(header);
        section.AddChild(body);
        AddChild(section);
        return body;
    }

    public void AddJob(BoxContainer section, JobPrototype job, string? count = null)
    {
        var row = CreateRow();

        if (_prototypes.TryIndex(job.Icon, out var icon))
            row.AddChild(CreateJobIcon(icon));

        var name = CreateLabel(job.LocalizedName, NebulaStyleClass.TextValue);
        name.HorizontalExpand = true;
        row.AddChild(name);

        if (count != null)
            row.AddChild(CreateLabel(count, NebulaStyleClass.TextMuted));

        section.AddChild(row);
    }

    /// <summary>
    /// Adds a person, indented under a job row when it has no icon of its own.
    /// </summary>
    public void AddPerson(BoxContainer section, string name, string? detail, ProtoId<JobIconPrototype>? icon = null)
    {
        var row = CreateRow();

        if (icon != null && _prototypes.TryIndex(icon.Value, out var jobIcon))
            row.AddChild(CreateJobIcon(jobIcon));
        else
            row.Margin = new Thickness(PersonIndent, 0, 0, 0);

        row.AddChild(CreateLabel(name, NebulaStyleClass.TextValue));

        if (!string.IsNullOrEmpty(detail))
        {
            var detailLabel = CreateLabel(detail, NebulaStyleClass.TextMuted);
            detailLabel.HorizontalExpand = true;
            detailLabel.ClipText = true;
            row.AddChild(detailLabel);
        }

        section.AddChild(row);
    }

    public void AddMessage(string text)
    {
        var label = CreateLabel(text, NebulaStyleClass.TextMuted);
        label.Align = Label.AlignMode.Center;
        label.Margin = new Thickness(0, 16);
        AddChild(label);
    }

    private static BoxContainer CreateRow()
    {
        return new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            MinHeight = 22,
        };
    }

    private static Label CreateLabel(string text, string styleClass)
    {
        var label = new Label { Text = text, VerticalAlignment = VAlignment.Center };
        label.AddStyleClass(styleClass);
        return label;
    }

    private static TextureRect CreateIcon(Texture texture, Color? color)
    {
        var icon = new TextureRect
        {
            Texture = texture,
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            SetSize = new Vector2(IconSize + 2, IconSize + 2),
            VerticalAlignment = VAlignment.Center,
            ModulateSelfOverride = color,
        };

        icon.AddStyleClass(NebulaStyleClass.Icon);
        return icon;
    }

    public TextureRect CreateJobIcon(JobIconPrototype icon)
    {
        return new TextureRect
        {
            Texture = _entitySystems.GetEntitySystem<SpriteSystem>().Frame0(icon.Icon),
            TextureScale = new Vector2(2, 2),
            SetSize = new Vector2(IconSize, IconSize),
            VerticalAlignment = VAlignment.Center,
        };
    }
}
