// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Client._Nebula.UserInterface;

/// <summary>
/// Style classes of the Nebula interface, see <see cref="NebulaSheetlet"/>.
/// </summary>
public static class NebulaStyleClass
{
    // panels
    public const string WindowPanel = "NebulaWindowPanel";
    public const string Panel = "NebulaPanel";
    public const string Inset = "NebulaInset";
    public const string Divider = "NebulaDivider";

    // labels
    public const string WindowTitle = "NebulaWindowTitle";
    public const string Heading = "NebulaHeading";
    public const string Title = "NebulaTitle";
    public const string Text = "NebulaText";
    public const string Muted = "NebulaMuted";

    // buttons, combine with the default "button" class that Button and OptionButton already have
    public const string Button = "NebulaButton";
    public const string ToggleButton = "NebulaToggleButton";
    public const string DangerButton = "NebulaDangerButton";
    public const string CloseButton = "NebulaCloseButton";

    // inputs
    public const string LineEdit = "NebulaLineEdit";
    public const string CheckBox = "NebulaCheckBox";
}
