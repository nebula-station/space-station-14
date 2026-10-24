// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Client._Nebula.UserInterface.Shapes;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Nebula.Stylesheets.Sheetlets;

/// <summary>
/// Nebula style classes colored from <see cref="NebulaThemeManager.Palette"/>, laid out like end-4's bar:
/// layer 0 for the bar and panels, layer 1 for groups and sections, secondary container for toggled things
/// and primary for the main accent.
/// </summary>
[CommonSheetlet]
public sealed class NebulaBaseSheetlet : Sheetlet<PalettedStylesheet>
{
    private const string PillTexture = "/Textures/_Nebula/Interface/pill.png";

    /// <summary>
    /// Shrinks the 32px pill texture so its rounded ends fit buttons about 24px tall.
    /// </summary>
    private const float PillScale = 0.75f;

    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var p = IoCManager.Resolve<NebulaThemeManager>().Palette;
        var pill = IoCManager.Resolve<IResourceCache>().GetTexture(PillTexture);

        return
        [
            E<PanelContainer>().Class(NebulaStyleClass.ScreenBase)
                .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat { BackgroundColor = p.Background }),

            E<PanelContainer>().Class(NebulaStyleClass.Divider)
                .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat { BackgroundColor = p.OutlineVariant }),

            E<CutoutFrame>().Class(NebulaStyleClass.ScreenFrame)
                .Prop(CutoutFrame.StylePropertyBackgroundColor, p.Layer0)
                .Prop(CutoutFrame.StylePropertyHoleShape, NebulaShapes.Screen),

            E<CutoutFrame>().Class(NebulaStyleClass.ScreenCorners)
                .Prop(CutoutFrame.StylePropertyBackgroundColor, Color.Black)
                .Prop(CutoutFrame.StylePropertyHoleShape, NebulaShapes.Screen),

            Panel(NebulaStyleClass.Panel, p.Layer0, NebulaShapes.Panel, p.Layer0Border),
            Panel(NebulaStyleClass.Section, p.Layer1, NebulaShapes.Section),
            Panel(NebulaStyleClass.Card, p.Layer2, NebulaShapes.Card),
            Panel(NebulaStyleClass.BarGroup, p.Layer1, NebulaShapes.Bar),
            Panel(NebulaStyleClass.BarCorner, Color.Transparent, NebulaShapes.Bar),
            Panel(NebulaStyleClass.BarCornerActive, p.SecondaryContainer, NebulaShapes.Bar),
            Panel(NebulaStyleClass.Popup, p.SurfaceContainer, NebulaShapes.Card, p.Layer0Border),
            Panel(NebulaStyleClass.Window, p.Layer0Base, NebulaShapes.Panel, p.Layer0Border),

            E<Label>().Class(NebulaStyleClass.Title)
                .Font(sheet.BaseFont.GetFont(13, FontKind.Bold))
                .FontColor(p.Primary),

            E<Label>().Class(NebulaStyleClass.TextMuted)
                .FontColor(p.Outline),

            E<Label>().Class(NebulaStyleClass.TextValue)
                .FontColor(p.OnSurface),

            E<Label>().Class(NebulaStyleClass.BarText)
                .FontColor(p.OnLayer1),

            E<Label>().Class(NebulaStyleClass.BarCaption)
                .Font(sheet.BaseFont.GetFont(9))
                .FontColor(p.Outline),

            E<Label>().Class(NebulaStyleClass.BarTitle)
                .Font(sheet.BaseFont.GetFont(12))
                .FontColor(p.OnLayer0),

            E<TextureRect>().Class(NebulaStyleClass.Icon)
                .Prop(Control.StylePropertyModulateSelf, p.OnSurfaceVariant),

            E<TextureRect>().Class(NebulaStyleClass.BarIcon)
                .Prop(Control.StylePropertyModulateSelf, p.OnLayer1),

            E<TextureRect>().Class(NebulaStyleClass.BarLogo)
                .Prop(Control.StylePropertyModulateSelf, p.OnLayer0),

            E<ShapedPanel>().Class(NebulaStyleClass.BarCornerActive)
                .ParentOf(E<TextureRect>().Class(NebulaStyleClass.BarLogo))
                .Prop(Control.StylePropertyModulateSelf, p.OnSecondaryContainer),

            E<ShapedPanel>().Class(NebulaStyleClass.BarCornerActive)
                .ParentOf(E<ShapedButton>().Class(NebulaStyleClass.ButtonBar))
                .Prop(ShapedButton.StylePropertyTextColor, p.OnSecondaryContainer),

            Button(NebulaStyleClass.ButtonTonal,
                idle: p.SecondaryContainer,
                hover: p.SecondaryContainerHover,
                pressed: p.SecondaryContainerActive,
                text: p.OnSecondaryContainer,
                active: p.Primary,
                activeText: p.OnPrimary),

            Button(NebulaStyleClass.ButtonFilled,
                idle: p.PrimaryContainer,
                hover: p.PrimaryContainerHover,
                pressed: p.PrimaryContainerActive,
                text: p.OnPrimaryContainer,
                active: p.Primary,
                activeText: p.OnPrimary,
                pulse: true),

            Button(NebulaStyleClass.ButtonGhost,
                idle: Color.Transparent,
                hover: p.Layer1Hover,
                pressed: p.Layer1Active,
                text: p.OnLayer1,
                active: p.SecondaryContainer,
                activeText: p.OnSecondaryContainer),

            Button(NebulaStyleClass.ButtonBar,
                idle: Color.Transparent,
                hover: p.Layer1Hover,
                pressed: p.Layer1Active,
                text: p.OnLayer0,
                active: p.SecondaryContainer,
                activeText: p.OnSecondaryContainer,
                shape: NebulaShapes.Bar),

            Button(NebulaStyleClass.ButtonBarAccent,
                idle: p.PrimaryContainer,
                hover: p.PrimaryContainerHover,
                pressed: p.PrimaryContainerActive,
                text: p.OnPrimaryContainer,
                active: p.Primary,
                activeText: p.OnPrimary,
                pulse: true,
                shape: NebulaShapes.Bar),

            Button(NebulaStyleClass.ButtonSupport,
                idle: p.TertiaryContainer,
                hover: NebulaPalette.Mix(p.TertiaryContainer, p.OnTertiaryContainer, 0.9f),
                pressed: NebulaPalette.Mix(p.TertiaryContainer, p.OnTertiaryContainer, 0.8f),
                text: p.OnTertiaryContainer,
                active: p.TertiaryContainer,
                activeText: p.OnTertiaryContainer,
                shape: NebulaShapes.Bar),

            Button(NebulaStyleClass.ButtonListItem,
                idle: Color.Transparent,
                hover: p.Layer2Hover,
                pressed: p.Layer2Active,
                text: p.OnLayer1,
                active: p.SecondaryContainer,
                activeText: p.OnSecondaryContainer),

            Button(NebulaStyleClass.ButtonChip,
                idle: p.Layer2,
                hover: p.Layer2Hover,
                pressed: p.Layer2Active,
                text: p.OnLayer2,
                active: p.SecondaryContainer,
                activeText: p.OnSecondaryContainer),

            Negative(NebulaStyleClass.ButtonTonal, p),
            Negative(NebulaStyleClass.ButtonBar, p),

            ..Accent(NebulaStyleClass.AccentError, p.Error, p.ErrorContainer, p.ErrorContainerHover,
                p.ErrorContainerActive, p.OnErrorContainer),
            ..Accent(NebulaStyleClass.AccentInfo, p.Info, p.InfoContainer, p.InfoContainerHover,
                p.InfoContainerActive, p.OnInfoContainer),

            Panel(NebulaStyleClass.ProgressTrack, p.SurfaceContainerHighest, NebulaShapes.Bar),
            Panel(NebulaStyleClass.ProgressFill, p.Primary, NebulaShapes.Bar),

            Panel(NebulaStyleClass.ChatInput, p.Layer2, NebulaShapes.Bar),

            E<LineEdit>().Class(NebulaStyleClass.ChatLineEdit)
                .Prop(LineEdit.StylePropertyStyleBox, new StyleBoxEmpty())
                .FontColor(p.OnSurface),

            E<ContainerButton>().Class(NebulaStyleClass.ChatChip).PseudoNormal()
                .Prop(ContainerButton.StylePropertyStyleBox, Chip(pill, Color.White.WithAlpha(0.14f))),
            E<ContainerButton>().Class(NebulaStyleClass.ChatChip).PseudoHovered()
                .Prop(ContainerButton.StylePropertyStyleBox, Chip(pill, Color.White.WithAlpha(0.22f))),
            E<ContainerButton>().Class(NebulaStyleClass.ChatChip).PseudoPressed()
                .Prop(ContainerButton.StylePropertyStyleBox, Chip(pill, Color.White.WithAlpha(0.3f))),

            E<ContainerButton>().Class(NebulaStyleClass.ChatIconButton).PseudoNormal()
                .Prop(ContainerButton.StylePropertyStyleBox, Chip(pill, Color.Transparent)),
            E<ContainerButton>().Class(NebulaStyleClass.ChatIconButton).PseudoHovered()
                .Prop(ContainerButton.StylePropertyStyleBox, Chip(pill, p.Layer2Hover)),
            E<ContainerButton>().Class(NebulaStyleClass.ChatIconButton).PseudoPressed()
                .Prop(ContainerButton.StylePropertyStyleBox, Chip(pill, p.Layer2Active)),
        ];
    }

    /// <summary>
    /// Pill-shaped box from a tinted circle texture, for vanilla buttons that can't use <see cref="ShapedButton"/>.
    /// </summary>
    /// <remarks>
    /// The chat chip is white, the channel selector tints it with the channel color through Modulate.
    /// </remarks>
    private static StyleBoxTexture Chip(Texture pill, Color color)
    {
        var box = new StyleBoxTexture
        {
            Texture = pill,
            Modulate = color,
            TextureScale = new Vector2(PillScale),
        };

        box.SetPatchMargin(StyleBox.Margin.All, 15);
        box.SetContentMarginOverride(StyleBox.Margin.Horizontal, 10);
        box.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
        return box;
    }

    private static StyleRule Panel(string styleClass, Color background, ShapeStyle shape, Color? border = null)
    {
        var rule = E<ShapedPanel>().Class(styleClass)
            .Prop(ShapedPanel.StylePropertyBackgroundColor, background)
            .Prop(ShapedPanel.StylePropertyShape, shape);

        if (border is { } borderColor)
        {
            rule = rule
                .Prop(ShapedPanel.StylePropertyBorderColor, borderColor)
                .Prop(ShapedPanel.StylePropertyBorderThickness, 1f);
        }

        return rule;
    }

    private static StyleRule Button(
        string styleClass,
        Color idle,
        Color hover,
        Color pressed,
        Color text,
        Color active,
        Color activeText,
        bool pulse = false,
        ShapeStyle? shape = null)
    {
        return E<ShapedButton>().Class(styleClass)
            .Prop(ShapedButton.StylePropertyIdleColor, idle)
            .Prop(ShapedButton.StylePropertyHoverColor, hover)
            .Prop(ShapedButton.StylePropertyPressedColor, pressed)
            .Prop(ShapedButton.StylePropertyDisabledColor, idle.A > 0f ? idle.WithAlpha(idle.A * 0.38f) : Color.Transparent)
            .Prop(ShapedButton.StylePropertyTextColor, text)
            .Prop(ShapedButton.StylePropertyActiveColor, active)
            .Prop(ShapedButton.StylePropertyActivePulseColor, NebulaPalette.Mix(active, activeText, 0.8f))
            .Prop(ShapedButton.StylePropertyActiveTextColor, activeText)
            .Prop(ShapedButton.StylePropertyPulseEnabled, pulse)
            .Prop(ShapedButton.StylePropertyShape, shape ?? NebulaShapes.Control);
    }

    private static StyleRule[] Accent(
        string accentClass,
        Color color,
        Color container,
        Color hover,
        Color pressed,
        Color onContainer)
    {
        return
        [
            E<Label>().Class(NebulaStyleClass.Title).Class(accentClass)
                .FontColor(color),

            E<TextureRect>().Class(NebulaStyleClass.Icon).Class(accentClass)
                .Prop(Control.StylePropertyModulateSelf, color),

            E<ShapedPanel>().Class(NebulaStyleClass.Card).Class(accentClass)
                .Prop(ShapedPanel.StylePropertyBorderColor, color.WithAlpha(0.5f))
                .Prop(ShapedPanel.StylePropertyBorderThickness, 1f),

            E<ShapedButton>().Class(NebulaStyleClass.ButtonFilled).Class(accentClass)
                .Prop(ShapedButton.StylePropertyIdleColor, container)
                .Prop(ShapedButton.StylePropertyHoverColor, hover)
                .Prop(ShapedButton.StylePropertyPressedColor, pressed)
                .Prop(ShapedButton.StylePropertyDisabledColor, container.WithAlpha(container.A * 0.38f))
                .Prop(ShapedButton.StylePropertyTextColor, onContainer),
        ];
    }

    private static StyleRule Negative(string styleClass, NebulaPalette p)
    {
        return E<ShapedButton>().Class(styleClass).Class(StyleClass.Negative)
            .Prop(ShapedButton.StylePropertyIdleColor, p.ErrorContainer)
            .Prop(ShapedButton.StylePropertyHoverColor, p.ErrorContainerHover)
            .Prop(ShapedButton.StylePropertyPressedColor, p.ErrorContainerActive)
            .Prop(ShapedButton.StylePropertyTextColor, p.OnErrorContainer);
    }
}
