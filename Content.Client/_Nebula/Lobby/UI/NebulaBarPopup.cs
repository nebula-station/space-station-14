// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.Lobby.UI;

/// <summary>
/// Hover popup shown under a bar group, with a list of icon rows.
/// </summary>
public sealed class NebulaBarPopup : Control
{
    private const float Gap = 6f;
    private const float IconSize = 16f;

    private readonly BoxContainer _rows;

    public NebulaBarPopup()
    {
        var panel = new ShapedPanel { Orientation = BoxContainer.LayoutOrientation.Vertical };
        panel.AddStyleClass(NebulaStyleClass.Popup);

        _rows = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(12, 10),
            SeparationOverride = 4,
        };

        panel.AddChild(_rows);
        AddChild(panel);
    }

    public void AddHeader(string text)
    {
        var label = new Label { Text = text };
        label.AddStyleClass(NebulaStyleClass.TextValue);
        _rows.AddChild(label);
    }

    public void AddRow(Texture? icon, string text, string? value = null, Color? iconColor = null, bool muted = false)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
        };

        if (icon != null)
        {
            var iconRect = new TextureRect
            {
                Texture = icon,
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
                SetSize = new Vector2(IconSize, IconSize),
                VerticalAlignment = VAlignment.Center,
                ModulateSelfOverride = iconColor,
            };

            iconRect.AddStyleClass(NebulaStyleClass.Icon);
            row.AddChild(iconRect);
        }

        var label = new Label { Text = text, HorizontalExpand = true };
        label.AddStyleClass(muted ? NebulaStyleClass.TextMuted : NebulaStyleClass.TextValue);
        row.AddChild(label);

        if (value != null)
        {
            var valueLabel = new Label
            {
                Text = value,
                Align = Label.AlignMode.Right,
                Margin = new Thickness(24, 0, 0, 0),
            };
            valueLabel.AddStyleClass(NebulaStyleClass.TextValue);
            row.AddChild(valueLabel);
        }

        _rows.AddChild(row);
    }

    public void AddMessage(FormattedMessage message, float maxWidth = 320f)
    {
        var label = new RichTextLabel { MaxWidth = maxWidth };
        label.SetMessage(message);
        _rows.AddChild(label);
    }

    /// <summary>
    /// Shows the popup built by <paramref name="build"/> under <paramref name="anchor"/> while it is hovered.
    /// </summary>
    public static void Attach(Control anchor, Func<NebulaBarPopup?> build)
    {
        anchor.TooltipDelay = 0.15f;
        anchor.TooltipSupplier = _ => build();
        anchor.OnShowTooltip += (_, _) =>
        {
            if (anchor.SuppliedTooltip is { } popup)
                PlaceUnder(anchor, popup);
        };
    }

    /// <summary>
    /// The engine puts tooltips at the cursor, move it to sit centered under the anchor instead.
    /// </summary>
    private static void PlaceUnder(Control anchor, Control popup)
    {
        popup.Measure(new Vector2(float.PositiveInfinity, float.PositiveInfinity));

        var size = popup.DesiredSize;
        var screen = popup.Parent?.Size ?? anchor.UserInterfaceManager.RootControl.Size;
        var x = anchor.GlobalPosition.X + (anchor.Size.X - size.X) / 2f;
        var y = anchor.GlobalPosition.Y + anchor.Size.Y + Gap;

        x = Math.Clamp(x, Gap, MathF.Max(Gap, screen.X - size.X - Gap));

        LayoutContainer.SetPosition(popup, new Vector2(x, y));
    }
}
