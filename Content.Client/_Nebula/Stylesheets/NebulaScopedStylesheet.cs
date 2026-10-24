// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.UserInterface.Shapes;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Nebula.Stylesheets;

/// <summary>
/// Interface stylesheet with vanilla buttons, fields, tabs, sliders and lists recolored from the Nebula palette.
/// Set it as <see cref="Control.Stylesheet"/> of a subtree that hosts upstream controls, such as the profile editor,
/// so they match Nebula without touching the rest of the game.
/// </summary>
public static class NebulaScopedStylesheet
{
    private const string OptionTriangle = OptionButton.StyleClassOptionTriangle;

    private static readonly string[] ButtonPseudos =
    [
        ContainerButton.StylePseudoClassNormal,
        ContainerButton.StylePseudoClassHover,
        ContainerButton.StylePseudoClassPressed,
        ContainerButton.StylePseudoClassDisabled,
    ];

    /// <summary>
    /// Vanilla button variants and whether their left and right ends stay rounded.
    /// </summary>
    private static readonly (string? StyleClass, bool Left, bool Right)[] ButtonVariants =
    [
        (null, true, true),
        (StyleClass.ButtonOpenLeft, false, true),
        (StyleClass.ButtonOpenRight, true, false),
        (StyleClass.ButtonOpenBoth, false, false),
        (StyleClass.ButtonSquare, false, false),
    ];

    /// <summary>
    /// Builds a new sheet on every call, use <see cref="NebulaThemeManager.GetScopedStylesheet"/> for the cached one.
    /// </summary>
    public static Stylesheet Create(Stylesheet baseSheet, NebulaPalette p)
    {
        var rules = new List<StyleRule>(baseSheet.Rules);
        AddButtonRules(rules, p);
        AddFieldRules(rules, p);
        AddContainerRules(rules, p);

        return new Stylesheet(rules);
    }

    private static void AddButtonRules(List<StyleRule> rules, NebulaPalette p)
    {
        foreach (var (variant, left, right) in ButtonVariants)
        {
            var shape = ControlShape(left, right);

            foreach (var pseudo in ButtonPseudos)
            {
                var (fill, text) = ButtonColors(pseudo, p.SecondaryContainer, p.SecondaryContainerHover,
                    p.Primary, p.OnSecondaryContainer, p.OnPrimary);

                AddButtonState(rules, variant, pseudo, fill, text, shape);
            }
        }

        foreach (var pseudo in ButtonPseudos)
        {
            var (negativeFill, negativeText) = ButtonColors(pseudo, p.ErrorContainer, p.ErrorContainerHover,
                p.ErrorContainerActive, p.OnErrorContainer, p.OnErrorContainer);
            AddButtonState(rules, StyleClass.Negative, pseudo, negativeFill, negativeText, NebulaShapes.Control);

            var (positiveFill, positiveText) = ButtonColors(pseudo, p.PrimaryContainer, p.PrimaryContainerHover,
                p.PrimaryContainerActive, p.OnPrimaryContainer, p.OnPrimaryContainer);
            AddButtonState(rules, StyleClass.Positive, pseudo, positiveFill, positiveText, NebulaShapes.Control);
        }

        rules.Add(E<TextureButton>().PseudoNormal().Modulate(p.OnSurfaceVariant));
        rules.Add(E<TextureButton>().PseudoHovered().Modulate(p.OnSurface));
        rules.Add(E<TextureButton>().PseudoPressed().Modulate(p.Primary));
    }

    private static (Color Fill, Color Text) ButtonColors(
        string pseudo,
        Color idle,
        Color hover,
        Color pressed,
        Color text,
        Color pressedText)
    {
        return pseudo switch
        {
            ContainerButton.StylePseudoClassHover => (hover, text),
            ContainerButton.StylePseudoClassPressed => (pressed, pressedText),
            ContainerButton.StylePseudoClassDisabled => (idle.WithAlpha(idle.A * 0.38f), text.WithAlpha(0.38f)),
            _ => (idle, text),
        };
    }

    private static void AddButtonState(
        List<StyleRule> rules,
        string? variant,
        string pseudo,
        Color fill,
        Color text,
        ShapeStyle shape)
    {
        rules.Add(Button(variant, pseudo)
            .Box(Box(fill, shape, 12, 3))
            .Prop(Control.StylePropertyModulateSelf, Color.White));

        rules.Add(Button(variant, pseudo).ParentOf(E<Label>()).FontColor(text));
        rules.Add(Button(variant, pseudo).ParentOf(E()).ParentOf(E<Label>()).FontColor(text));
        rules.Add(Button(variant, pseudo).ParentOf(E()).ParentOf(E<TextureRect>().Class(OptionTriangle))
            .Prop(Control.StylePropertyModulateSelf, text));
    }

    private static MutableSelectorElement Button(string? variant, string pseudo)
    {
        return E<ContainerButton>()
            .Class(ContainerButton.StyleClassButton)
            .MaybeClass(variant)
            .Pseudo(pseudo);
    }

    private static void AddFieldRules(List<StyleRule> rules, NebulaPalette p)
    {
        var lineEdit = Box(p.Layer2, NebulaShapes.Control, 8, 3);
        lineEdit.BorderColor = p.OutlineVariant;
        lineEdit.BorderThickness = 1f;

        rules.Add(E<LineEdit>()
            .Prop(LineEdit.StylePropertyStyleBox, lineEdit)
            .Prop(LineEdit.StylePropertyCursorColor, p.Primary)
            .Prop(LineEdit.StylePropertySelectionColor, p.Primary.WithAlpha(0.35f))
            .FontColor(p.OnSurface));
        rules.Add(E<LineEdit>().Class(LineEdit.StyleClassLineEditNotEditable).FontColor(p.OnSurfaceVariant));
        rules.Add(E<LineEdit>().Pseudo(LineEdit.StylePseudoClassPlaceholder).FontColor(p.Outline));
        rules.Add(E<TextEdit>().Pseudo(TextEdit.StylePseudoClassPlaceholder).FontColor(p.Outline));

        var track = new Thickness(0, 5);
        var grabber = Box(p.Primary, ShapeStyle.Pill, 8, 8);

        rules.Add(E<Slider>()
            .Prop(Slider.StylePropertyBackground, new ShapedStyleBox
            {
                BackgroundColor = p.SurfaceContainerHighest,
                Shape = ShapeStyle.Pill,
                Inset = track,
            })
            .Prop(Slider.StylePropertyFill, new ShapedStyleBox
            {
                BackgroundColor = p.Primary,
                Shape = ShapeStyle.Pill,
                Inset = track,
            })
            .Prop(Slider.StylePropertyForeground, new StyleBoxEmpty())
            .Prop(Slider.StylePropertyGrabber, grabber));
    }

    private static void AddContainerRules(List<StyleRule> rules, NebulaPalette p)
    {
        rules.Add(E<Label>().FontColor(p.OnSurface));

        rules.Add(E<TabContainer>()
            .Prop(TabContainer.StylePropertyPanelStyleBox, Box(p.Layer1, NebulaShapes.Section, 10, 10))
            .Prop(TabContainer.StylePropertyTabStyleBox, Box(p.SecondaryContainer, ShapeStyle.Pill, 14, 4))
            .Prop(TabContainer.StylePropertyTabStyleBoxInactive, Box(Color.Transparent, ShapeStyle.Pill, 14, 4))
            .Prop(TabContainer.stylePropertyTabFontColor, p.OnSecondaryContainer)
            .Prop(TabContainer.StylePropertyTabFontColorInactive, p.OnLayer1Inactive));

        rules.Add(E<PanelContainer>().Class(NebulaStyleClass.VanillaSection)
            .Panel(Box(p.Layer1, NebulaShapes.Section, 10, 10)));

        rules.Add(E<PanelContainer>().Class(NebulaStyleClass.VanillaHeader)
            .Panel(Box(p.Layer2, NebulaShapes.Card, 10, 5)));
        rules.Add(E<PanelContainer>().Class(NebulaStyleClass.VanillaHeader).ParentOf(E<Label>())
            .FontColor(p.Primary));

        rules.Add(E<Separator>().Prop(Separator.StylePropertyColor, p.OutlineVariant));
        rules.Add(E<Separator>().Class(StyleClass.LowDivider).Prop(Separator.StylePropertyColor, p.OutlineVariant));

        rules.Add(E<ItemList>()
            .Prop(ItemList.StylePropertyBackground, Box(p.Layer2, NebulaShapes.Card, 4, 4))
            .Prop(ItemList.StylePropertyItemBackground, Box(Color.Transparent, NebulaShapes.Control, 6, 2))
            .Prop(ItemList.StylePropertySelectedItemBackground, Box(p.SecondaryContainer, NebulaShapes.Control, 6, 2))
            .Prop(ItemList.StylePropertyDisabledItemBackground, Box(Color.Transparent, NebulaShapes.Control, 6, 2))
            .FontColor(p.OnSurface));

        AddScrollBarRules<VScrollBar>(rules, p, new Thickness(3, 2));
        AddScrollBarRules<HScrollBar>(rules, p, new Thickness(2, 3));
    }

    private static void AddScrollBarRules<T>(List<StyleRule> rules, NebulaPalette p, Thickness inset)
        where T : ScrollBar
    {
        rules.Add(E<T>().Prop(ScrollBar.StylePropertyGrabber, Grabber(p.OutlineVariant, inset)));
        rules.Add(E<T>().PseudoHovered().Prop(ScrollBar.StylePropertyGrabber, Grabber(p.Outline, inset)));
        rules.Add(E<T>().PseudoPressed().Prop(ScrollBar.StylePropertyGrabber, Grabber(p.OnSurfaceVariant, inset)));
    }

    private static ShapedStyleBox Grabber(Color color, Thickness inset)
    {
        var box = new ShapedStyleBox
        {
            BackgroundColor = color,
            Shape = ShapeStyle.Pill,
            Inset = inset,
        };

        box.SetContentMarginOverride(StyleBox.Margin.Left, 10);
        box.SetContentMarginOverride(StyleBox.Margin.Top, 10);
        return box;
    }

    private static ShapeStyle ControlShape(bool left, bool right)
    {
        var shape = NebulaShapes.Control;

        return new ShapeStyle(
            left ? shape.TopLeft : Corner.Square,
            right ? shape.TopRight : Corner.Square,
            right ? shape.BottomRight : Corner.Square,
            left ? shape.BottomLeft : Corner.Square);
    }

    private static ShapedStyleBox Box(Color color, ShapeStyle shape, float horizontal, float vertical)
    {
        var box = new ShapedStyleBox
        {
            BackgroundColor = color,
            Shape = shape,
        };

        box.SetContentMarginOverride(StyleBox.Margin.Horizontal, horizontal);
        box.SetContentMarginOverride(StyleBox.Margin.Vertical, vertical);
        return box;
    }
}
