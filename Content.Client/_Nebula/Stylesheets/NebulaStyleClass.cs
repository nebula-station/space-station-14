// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Client._Nebula.Stylesheets;

public static class NebulaStyleClass
{
    public const string ScreenBase = "NebulaScreenBase";
    public const string ScreenFrame = "NebulaScreenFrame";
    public const string ScreenCorners = "NebulaScreenCorners";

    public const string Panel = "NebulaPanel";
    public const string Section = "NebulaSection";
    public const string Card = "NebulaCard";
    public const string BarGroup = "NebulaBarGroup";
    public const string BarCorner = "NebulaBarCorner";
    /// <summary>
    /// Toggled bar corner, its content sits on secondary container like end-4's toggled bar buttons.
    /// </summary>
    public const string BarCornerActive = "NebulaBarCornerActive";
    public const string Divider = "NebulaDivider";
    public const string Popup = "NebulaPopup";
    public const string Window = "NebulaWindow";

    public const string Title = "NebulaTitle";
    public const string TextMuted = "NebulaTextMuted";
    public const string TextValue = "NebulaTextValue";
    public const string BarCaption = "NebulaBarCaption";
    public const string BarTitle = "NebulaBarTitle";
    public const string BarText = "NebulaBarText";

    public const string Icon = "NebulaIcon";
    public const string BarIcon = "NebulaBarIcon";
    public const string BarLogo = "NebulaBarLogo";

    public const string ButtonTonal = "NebulaButtonTonal";
    public const string ButtonFilled = "NebulaButtonFilled";
    public const string ButtonGhost = "NebulaButtonGhost";
    public const string ButtonBar = "NebulaButtonBar";
    public const string ButtonBarAccent = "NebulaButtonBarAccent";
    public const string ButtonSupport = "NebulaButtonSupport";
    public const string ButtonListItem = "NebulaButtonListItem";
    public const string ButtonChip = "NebulaButtonChip";

    /// <summary>
    /// Red accent for <see cref="Title"/>, <see cref="Icon"/>, <see cref="Card"/> and <see cref="ButtonFilled"/>.
    /// </summary>
    public const string AccentError = "NebulaAccentError";
    /// <summary>
    /// Blue accent, see <see cref="AccentError"/>.
    /// </summary>
    public const string AccentInfo = "NebulaAccentInfo";

    public const string ProgressTrack = "NebulaProgressTrack";
    public const string ProgressFill = "NebulaProgressFill";

    public const string ChatInput = "NebulaChatInput";
    public const string ChatLineEdit = "NebulaChatLineEdit";
    public const string ChatChip = "NebulaChatChip";
    public const string ChatIconButton = "NebulaChatIconButton";

    /// <summary>
    /// Vanilla <see cref="Robust.Client.UserInterface.Controls.PanelContainer"/> drawn as a section,
    /// only styled under <see cref="NebulaScopedStylesheet"/>.
    /// </summary>
    public const string VanillaSection = "NebulaVanillaSection";

    /// <summary>
    /// Vanilla panel drawn as a list header card, only styled under <see cref="NebulaScopedStylesheet"/>.
    /// </summary>
    public const string VanillaHeader = "NebulaVanillaHeader";
}
