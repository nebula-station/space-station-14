// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Client._Nebula.Manifest;
using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Shared.Ghost.Roles;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.Ghost.Roles;

/// <summary>
/// Ghost roles sorted into <see cref="NebulaGhostRoleCategory"/> sections, antagonists first.
/// Replaces <see cref="Content.Client.UserInterface.Systems.Ghost.Controls.Roles.GhostRolesWindow"/>
/// and keeps its API, entries are laid out once per frame after they change.
/// </summary>
public sealed partial class NebulaGhostRolesWindow : NebulaWindow
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private NebulaThemeManager _theme = default!;

    public event Action<GhostRoleInfo>? OnRoleRequestButtonClicked;
    public event Action<GhostRoleInfo>? OnRoleFollow;

    private readonly NebulaManifestListing _listing = new();
    private readonly List<Entry> _entries = new();
    private readonly HashSet<(string Name, string Description)> _expanded = new();
    private bool _dirty = true;

    private sealed record Entry(
        string Name,
        string Description,
        bool HasAccess,
        FormattedMessage? Reason,
        List<GhostRoleInfo> Roles,
        NebulaGhostRoleCategory Category);

    public NebulaGhostRolesWindow()
    {
        IoCManager.InjectDependencies(this);

        Title = Loc.GetString("ghost-roles-window-title");
        SetSize = new Vector2(520, 640);
        MinSize = new Vector2(400, 360);

        Contents.AddChild(new ScrollContainer
        {
            VerticalExpand = true,
            HScrollEnabled = false,
            Children = { _listing },
        });
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

    public void ClearEntries()
    {
        _entries.Clear();
        _dirty = true;
    }

    /// <summary>
    /// Kept for <see cref="Content.Client.UserInterface.Systems.Ghost.Controls.Roles.GhostRolesEui"/>,
    /// expanded groups are remembered as they are toggled.
    /// </summary>
    public void SaveCollapsibleBoxesStates()
    {
    }

    /// <inheritdoc cref="SaveCollapsibleBoxesStates"/>
    public void RestoreCollapsibleBoxesStates()
    {
    }

    public void AddEntry(
        string name,
        string description,
        bool hasAccess,
        FormattedMessage? reason,
        IEnumerable<GhostRoleInfo> roles,
        SpriteSystem spriteSystem)
    {
        var list = roles.ToList();
        var category = list.Min(role => NebulaGhostRoleCategories.Get(role, _prototypes));

        _entries.Add(new Entry(name, description, hasAccess, reason, list, category));
        _dirty = true;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_dirty)
            return;

        _dirty = false;
        Rebuild();
    }

    private void OnPaletteChanged(NebulaPalette palette)
    {
        _dirty = true;
    }

    private void Rebuild()
    {
        _listing.Clear();

        if (_entries.Count == 0)
        {
            _listing.AddMessage(Loc.GetString("ghost-roles-window-no-roles-available-label"));
            return;
        }

        foreach (var category in _entries.GroupBy(entry => entry.Category).OrderBy(group => group.Key))
        {
            var accent = NebulaGhostRoleCategories.GetAccent(category.Key);
            var section = _listing.AddSection(NebulaGhostRoleCategories.GetIcon(category.Key),
                null,
                NebulaGhostRoleCategories.GetName(category.Key),
                category.Sum(entry => entry.Roles.Count).ToString(),
                accent);

            section.SeparationOverride = 6;

            foreach (var entry in category.OrderBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                section.AddChild(CreateCard(entry, accent));
            }
        }
    }

    private ShapedPanel CreateCard(Entry entry, string? accent)
    {
        var card = new ShapedPanel { Orientation = BoxContainer.LayoutOrientation.Vertical };
        card.AddStyleClass(NebulaStyleClass.Card);

        if (accent != null)
            card.AddStyleClass(accent);

        var body = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(10, 8),
            SeparationOverride = 6,
        };

        var name = new Label { Text = entry.Name };
        name.AddStyleClass(NebulaStyleClass.TextValue);
        body.AddChild(name);

        var description = new RichTextLabel();
        description.SetMessage(entry.Description, _theme.Palette.OnSurfaceVariant);
        body.AddChild(description);

        if (entry.Roles.Count == 1)
            body.AddChild(CreateButtons(entry, entry.Roles[0], accent));
        else
            AddGroup(body, entry, accent);

        card.AddChild(body);
        return card;
    }

    /// <summary>
    /// Several identical roles go under a toggle, like the vanilla collapsible.
    /// </summary>
    private void AddGroup(BoxContainer body, Entry entry, string? accent)
    {
        var key = (entry.Name, entry.Description);

        var rows = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
            Visible = _expanded.Contains(key),
        };

        foreach (var role in entry.Roles)
        {
            rows.AddChild(CreateButtons(entry, role, accent));
        }

        var toggle = new ShapedButton
        {
            Text = Loc.GetString("ghost-roles-window-available-button", ("rolesCount", entry.Roles.Count)),
            Icon = NebulaIcons.Get("group"),
            ToggleMode = true,
            Pressed = rows.Visible,
        };
        toggle.AddStyleClass(NebulaStyleClass.ButtonGhost);
        toggle.OnToggled += args =>
        {
            rows.Visible = args.Pressed;

            if (args.Pressed)
                _expanded.Add(key);
            else
                _expanded.Remove(key);
        };

        body.AddChild(toggle);
        body.AddChild(rows);
    }

    private NebulaGhostRoleButtons CreateButtons(Entry entry, GhostRoleInfo role, string? accent)
    {
        var buttons = new NebulaGhostRoleButtons(role, entry.HasAccess, entry.Reason, accent);
        buttons.RequestButton.OnPressed += _ => OnRoleRequestButtonClicked?.Invoke(role);
        buttons.FollowButton.OnPressed += _ => OnRoleFollow?.Invoke(role);
        return buttons;
    }
}
