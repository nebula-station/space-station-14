// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Client._Nebula.Ghost.Roles;
using Content.Client._Nebula.Lobby;
using Content.Client._Nebula.Manifest;
using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Shared._Nebula.Ghost;
using Content.Shared.Ghost.Systems;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Nebula.Ghost;

/// <summary>
/// Ghost warps as chips sorted into visible antagonists, departments, others and locations.
/// Replaces <see cref="Content.Client.UserInterface.Systems.Ghost.Controls.GhostTargetWindow"/> and keeps its API.
/// </summary>
public sealed partial class NebulaGhostWarpWindow : NebulaWindow
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private NebulaThemeManager _theme = default!;

    private const float ChipWidth = 168f;
    private const float ChipGap = 6f;
    private const float InactiveAlpha = 0.6f;

    /// <summary>
    /// Horizontal space a section takes around its grid, its body margin plus the scroll bar.
    /// </summary>
    private const float SectionPadding = 36f;

    public event Action<NetEntity>? WarpClicked;
    public event Action? OnGhostnadoClicked;
    public event Action? OnWarpToRandomFollowedClicked;
    public event Action? OnWarpToRandomClicked;

    private readonly LineEdit _search;
    private readonly ScrollContainer _scroll;
    private readonly NebulaManifestListing _listing = new();
    private readonly List<Section> _sections = new();
    private List<GhostWarp> _warps = new();
    private int _columns = 1;

    private sealed record Section(Control Panel, GridContainer Grid, List<(ShapedButton Chip, string Search)> Chips);

    public NebulaGhostWarpWindow()
    {
        IoCManager.InjectDependencies(this);

        Title = Loc.GetString("ghost-target-window-title");
        SetSize = new Vector2(900, 700);
        MinSize = new Vector2(520, 400);

        var quick = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
        };
        quick.AddChild(CreateQuickButton("ghost-target-window-warp-to-most-followed", "Ghost/trending_up",
            () => OnGhostnadoClicked?.Invoke()));
        quick.AddChild(CreateQuickButton("ghost-target-window-warp-to-random-followed", "group",
            () => OnWarpToRandomFollowedClicked?.Invoke()));
        quick.AddChild(CreateQuickButton("ghost-target-window-warp-to-random", "Ghost/shuffle",
            () => OnWarpToRandomClicked?.Invoke()));

        var searchIcon = new TextureRect
        {
            Texture = NebulaIcons.Get("Ghost/search"),
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            SetSize = new Vector2(16, 16),
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0),
        };
        searchIcon.AddStyleClass(NebulaStyleClass.Icon);

        _search = new LineEdit
        {
            HorizontalExpand = true,
            PlaceHolder = Loc.GetString("window-search-bar-placeholder"),
        };
        _search.AddStyleClass(NebulaStyleClass.ChatLineEdit);
        _search.OnTextChanged += _ => OnSearchChanged();

        var searchPanel = new ShapedPanel
        {
            Shape = NebulaShapes.Control,
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            MinHeight = 30,
            SeparationOverride = 4,
            Children = { searchIcon, _search },
        };
        searchPanel.AddStyleClass(NebulaStyleClass.ChatInput);

        _scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HScrollEnabled = false,
            Children = { _listing },
        };

        Contents.AddChild(quick);
        Contents.AddChild(searchPanel);
        Contents.AddChild(_scroll);
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        _theme.PaletteChanged += OnPaletteChanged;
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        _theme.PaletteChanged -= OnPaletteChanged;
    }

    public void UpdateWarps(IEnumerable<GhostWarp> warps)
    {
        _warps = warps.ToList();
    }

    public void Populate()
    {
        _listing.Clear();
        _sections.Clear();

        var palette = _theme.Palette;
        var players = _warps.Where(warp => !warp.IsWarpPoint).ToList();
        var departments = _prototypes.EnumeratePrototypes<DepartmentPrototype>().ToList();
        departments.Sort(DepartmentUIComparer.Instance);

        AddSection(NebulaGhostRoleCategories.GetIcon(NebulaGhostRoleCategory.Antagonist),
            null,
            NebulaGhostRoleCategories.GetName(NebulaGhostRoleCategory.Antagonist),
            NebulaStyleClass.AccentError,
            players.Where(warp => warp.IsVisibleAntagonist),
            (palette.ErrorContainer, palette.OnErrorContainer),
            NebulaIcons.Get("Ghost/skull"));

        var crew = players.Where(warp => !warp.IsVisibleAntagonist).ToList();
        var placed = new HashSet<NetEntity>();

        foreach (var department in departments)
        {
            var members = crew
                .Where(warp => warp.Job is { } job && !placed.Contains(warp.Entity) && department.Roles.Contains(job))
                .ToList();

            placed.UnionWith(members.Select(warp => warp.Entity));

            if (department.ID == "CentralCommand")
            {
                AddSection(NebulaGhostRoleCategories.GetIcon(NebulaGhostRoleCategory.CentralCommand),
                    null,
                    Loc.GetString(department.Name),
                    NebulaStyleClass.AccentInfo,
                    members,
                    (palette.InfoContainer, palette.OnInfoContainer));
                continue;
            }

            var colors = palette.GetCustomColor(department.Color);
            AddSection(NebulaDepartments.GetIcon(department),
                NebulaDepartments.GetColor(palette, department),
                Loc.GetString(department.Name),
                null,
                members,
                (colors.Container, colors.OnContainer));
        }

        AddSection(NebulaGhostRoleCategories.GetIcon(NebulaGhostRoleCategory.Other),
            null,
            NebulaGhostRoleCategories.GetName(NebulaGhostRoleCategory.Other),
            null,
            crew.Where(warp => !placed.Contains(warp.Entity)),
            null,
            NebulaIcons.Get("Departments/person"));

        AddSection(NebulaIcons.Get("Ghost/place"),
            null,
            Loc.GetString("nebula-ghost-warp-category-locations"),
            null,
            _warps.Where(warp => warp.IsWarpPoint),
            null,
            NebulaIcons.Get("Ghost/place"));

        if (_sections.Count == 0)
            _listing.AddMessage(Loc.GetString("nebula-ghost-warp-empty"));

        UpdateColumns(true);
        ApplySearch();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        UpdateColumns(false);
    }

    private void OnPaletteChanged(NebulaPalette palette)
    {
        Populate();
    }

    /// <param name="colors">Chip fill and text, null keeps the plain chip style.</param>
    /// <param name="icon">Monochrome chip icon for warps without a job icon.</param>
    private void AddSection(
        Texture sectionIcon,
        Color? sectionIconColor,
        string title,
        string? accent,
        IEnumerable<GhostWarp> warps,
        (Color Fill, Color Text)? colors,
        Texture? icon = null)
    {
        var sorted = warps
            .OrderBy(warp => warp.State)
            .ThenBy(warp => GetName(warp), StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        if (sorted.Count == 0)
            return;

        var body = _listing.AddSection(sectionIcon, sectionIconColor, title, sorted.Count.ToString(), accent);

        var grid = new GridContainer
        {
            Columns = _columns,
            HSeparationOverride = (int) ChipGap,
            VSeparationOverride = (int) ChipGap,
        };
        body.AddChild(grid);

        var panel = (ShapedPanel) body.Parent!;
        panel.Shape = NebulaShapes.Control;

        var section = new Section(panel, grid, new List<(ShapedButton, string)>());

        foreach (var warp in sorted)
        {
            var chip = CreateChip(warp, colors, icon);
            grid.AddChild(chip);
            section.Chips.Add((chip, GetDescription(warp)));
        }

        _sections.Add(section);
    }

    private ShapedButton CreateChip(GhostWarp warp, (Color Fill, Color Text)? colors, Texture? icon)
    {
        var chip = new ShapedButton
        {
            Text = GetName(warp),
            SetWidth = ChipWidth,
            MinHeight = 26,
            ToolTip = GetDescription(warp),
            ColorOverride = colors?.Fill,
            TextColorOverride = colors?.Text,
        };
        chip.AddStyleClass(NebulaStyleClass.ButtonChip);
        chip.Content.HorizontalAlignment = HAlignment.Stretch;
        chip.Content.Margin = new Thickness(8, 0);
        chip.Label.HorizontalExpand = true;
        chip.Label.ClipText = true;

        if (warp.Job is { } job
            && _prototypes.TryIndex(job, out var jobProto)
            && _prototypes.TryIndex(jobProto.Icon, out var jobIcon))
        {
            var jobRect = _listing.CreateJobIcon(jobIcon);
            chip.Content.AddChild(jobRect);
            jobRect.SetPositionFirst();
        }
        else if (icon != null)
        {
            chip.Icon = icon;
            chip.IconSize = 16;
        }

        if (GetStateIcon(warp.State) is { } stateIcon)
        {
            chip.Content.Modulate = Color.White.WithAlpha(InactiveAlpha);

            var stateRect = new TextureRect
            {
                Texture = NebulaIcons.Get(stateIcon),
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
                SetSize = new Vector2(14, 14),
                VerticalAlignment = VAlignment.Center,
                ModulateSelfOverride = colors?.Text,
            };
            stateRect.AddStyleClass(NebulaStyleClass.Icon);
            chip.Content.AddChild(stateRect);
        }

        chip.OnPressed += _ => WarpClicked?.Invoke(warp.Entity);
        return chip;
    }

    private static string? GetStateIcon(NebulaGhostWarpState state)
    {
        return state switch
        {
            NebulaGhostWarpState.Ssd => "Ghost/bedtime",
            NebulaGhostWarpState.Dead => "Ghost/skull",
            _ => null,
        };
    }

    /// <summary>
    /// Full name with the job and the SSD or dead mark, for the tooltip and the search.
    /// </summary>
    private static string GetDescription(GhostWarp warp)
    {
        return warp.State switch
        {
            NebulaGhostWarpState.Ssd => $"{warp.DisplayName} · {Loc.GetString("nebula-ghost-warp-ssd")}",
            NebulaGhostWarpState.Dead => $"{warp.DisplayName} · {Loc.GetString("nebula-ghost-warp-dead")}",
            _ => warp.DisplayName,
        };
    }

    private static string GetName(GhostWarp warp)
    {
        return warp.Name ?? warp.DisplayName;
    }

    private ShapedButton CreateQuickButton(string text, string icon, Action pressed)
    {
        var button = new ShapedButton
        {
            Text = Loc.GetString(text),
            Icon = NebulaIcons.Get(icon),
            ToolTip = Loc.GetString($"{text}-tooltip"),
            HorizontalExpand = true,
            MinHeight = 30,
        };
        button.AddStyleClass(NebulaStyleClass.ButtonTonal);
        button.OnPressed += _ => pressed();
        return button;
    }

    private void UpdateColumns(bool force)
    {
        var width = _scroll.Size.X - SectionPadding;
        var columns = Math.Max(1, (int) ((width + ChipGap) / (ChipWidth + ChipGap)));

        if (columns == _columns && !force)
            return;

        _columns = columns;

        foreach (var section in _sections)
        {
            section.Grid.Columns = columns;
        }
    }

    private void OnSearchChanged()
    {
        ApplySearch();
        _scroll.SetScrollValue(Vector2.Zero);
    }

    private void ApplySearch()
    {
        var text = _search.Text.Trim();

        foreach (var section in _sections)
        {
            var any = false;

            foreach (var (chip, search) in section.Chips)
            {
                chip.Visible = text.Length == 0 || search.Contains(text, StringComparison.OrdinalIgnoreCase);
                any |= chip.Visible;
            }

            section.Panel.Visible = any;
        }
    }
}
