// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Nebula.UserInterface;

[CommonSheetlet]
public sealed class NebulaSheetlet : Sheetlet<PalettedStylesheet>
{
    public const float PanelRadius = 8f;
    public const float InputRadius = 6f;

    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var lineEditBox = new NebulaStyleBox(NebulaPalette.Void.WithAlpha(0.8f), InputRadius, NebulaPalette.PurpleDim);
        lineEditBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 6);
        lineEditBox.SetContentMarginOverride(StyleBox.Margin.Vertical, 3);

        var rules = new List<StyleRule>
        {
            // panels
            E<PanelContainer>().Class(NebulaStyleClass.WindowPanel)
                .Panel(new NebulaStyleBox(NebulaPalette.Void.WithAlpha(0.96f), 10f, NebulaPalette.Border)),
            E<PanelContainer>().Class(NebulaStyleClass.Panel)
                .Panel(new NebulaStyleBox(NebulaPalette.Surface, PanelRadius)),
            E<PanelContainer>().Class(NebulaStyleClass.Inset)
                .Panel(new NebulaStyleBox(NebulaPalette.Void.WithAlpha(0.7f), PanelRadius, NebulaPalette.PurpleDim)),
            E<PanelContainer>().Class(NebulaStyleClass.Divider)
                .Panel(new StyleBoxFlat(NebulaPalette.PurpleDim))
                .MinHeight(1),

            // labels
            E<Label>().Class(NebulaStyleClass.WindowTitle)
                .FontColor(NebulaPalette.PurpleBright)
                .Font(sheet.BaseFont.GetFont(13, FontKind.Bold)),
            E<Label>().Class(NebulaStyleClass.Heading)
                .FontColor(NebulaPalette.Moonlight)
                .Font(sheet.BaseFont.GetFont(16, FontKind.Bold)),
            E<Label>().Class(NebulaStyleClass.Title)
                .FontColor(NebulaPalette.PurpleBright)
                .Font(sheet.BaseFont.GetFont(12, FontKind.Bold)),
            E<Label>().Class(NebulaStyleClass.Text)
                .FontColor(NebulaPalette.Moonlight),
            E<Label>().Class(NebulaStyleClass.Muted)
                .FontColor(NebulaPalette.MoonlightMuted)
                .Font(sheet.BaseFont.GetFont(10)),

            // inputs
            E<LineEdit>().Class(NebulaStyleClass.LineEdit)
                .Prop(LineEdit.StylePropertyStyleBox, lineEditBox)
                .Prop(LineEdit.StylePropertyCursorColor, NebulaPalette.Moonlight)
                .Prop(LineEdit.StylePropertySelectionColor, NebulaPalette.Purple.WithAlpha(0.5f))
                .FontColor(NebulaPalette.Moonlight),
            E<LineEdit>().Class(NebulaStyleClass.LineEdit)
                .Pseudo(LineEdit.StylePseudoClassPlaceholder)
                .FontColor(NebulaPalette.MoonlightMuted),
            E<CheckBox>().Class(NebulaStyleClass.CheckBox)
                .ParentOf(E<BoxContainer>())
                .ParentOf(E<Label>())
                .FontColor(NebulaPalette.Moonlight),

            // close button of NebulaWindow, the texture comes from the default window close button
            CloseButton().PseudoNormal().Modulate(NebulaPalette.Moonlight),
            CloseButton().PseudoHovered().Modulate(NebulaPalette.Magenta),
            CloseButton().PseudoPressed().Modulate(NebulaPalette.MagentaDim),
        };

        AddButtonRules(rules, NebulaStyleClass.Button,
            NebulaPalette.SurfaceLow, NebulaPalette.Hover, NebulaPalette.Pressed);
        AddButtonRules(rules, NebulaStyleClass.ToggleButton,
            NebulaPalette.SurfaceLow, NebulaPalette.Hover, NebulaPalette.Purple);
        AddButtonRules(rules, NebulaStyleClass.DangerButton,
            NebulaPalette.MagentaDim, NebulaPalette.Magenta, NebulaPalette.Magenta);

        return rules.ToArray();
    }

    private static void AddButtonRules(List<StyleRule> rules, string styleClass, Color idle, Color hover, Color pressed)
    {
        // the default button rules tint the whole box per state, the colors here are final
        rules.AddRange(
        [
            ButtonState(styleClass).PseudoNormal().Box(ButtonBox(idle)).Modulate(Color.White),
            ButtonState(styleClass).PseudoHovered().Box(ButtonBox(hover)).Modulate(Color.White),
            ButtonState(styleClass).PseudoPressed().Box(ButtonBox(pressed)).Modulate(Color.White),
            ButtonState(styleClass).PseudoDisabled().Box(ButtonBox(idle.WithAlpha(idle.A * 0.5f))).Modulate(Color.White),

            // Button holds its label directly, OptionButton inside a box with the arrow
            ButtonState(styleClass).ParentOf(E<Label>()).FontColor(NebulaPalette.Moonlight),
            ButtonState(styleClass).ParentOf(E()).ParentOf(E<Label>()).FontColor(NebulaPalette.Moonlight),
            ButtonState(styleClass).PseudoDisabled().ParentOf(E<Label>()).FontColor(NebulaPalette.MoonlightMuted),
            ButtonState(styleClass).PseudoDisabled().ParentOf(E()).ParentOf(E<Label>()).FontColor(NebulaPalette.MoonlightMuted),
        ]);
    }

    private static NebulaStyleBox ButtonBox(Color color)
    {
        var box = new NebulaStyleBox(color, PanelRadius);
        box.SetContentMarginOverride(StyleBox.Margin.Horizontal, 10);
        box.SetContentMarginOverride(StyleBox.Margin.Vertical, 3);
        return box;
    }

    private static MutableSelectorElement ButtonState(string styleClass)
    {
        return E<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(styleClass);
    }

    private static MutableSelectorElement CloseButton()
    {
        return E<TextureButton>().Class(DefaultWindow.StyleClassWindowCloseButton).Class(NebulaStyleClass.CloseButton);
    }
}
