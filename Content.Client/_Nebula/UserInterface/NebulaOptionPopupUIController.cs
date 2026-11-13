// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Nebula.UserInterface;

/// <summary>
/// Gives the list of an <see cref="OptionButton"/> the Nebula scoped stylesheet of the window the button sits in.
/// </summary>
public sealed partial class NebulaOptionPopupUIController : UIController
{
    [Dependency] private NebulaThemeManager _theme = default!;

    public override void Initialize()
    {
        base.Initialize();

        UIManager.ModalRoot.OnChildAdded += OnModalChildAdded;
    }

    private void OnModalChildAdded(Control child)
    {
        if (child is not Popup popup || !popup.HasStyleClass(OptionButton.StyleClassPopup))
            return;
        Stylesheet? sheet = null;
        for (var control = UIManager.CurrentlyHovered; control != null; control = control.Parent)
        {
            if (control is not OptionButton button || button.OptionsScroll.Parent != popup)
                continue;

            sheet = FindStylesheet(button);
            break;
        }

        var scoped = _theme.IsScopedStylesheet(sheet) ? sheet : null;
        if (popup.Stylesheet != scoped)
            popup.Stylesheet = scoped;
    }

    private static Stylesheet? FindStylesheet(Control control)
    {
        for (var current = control; current != null; current = current.Parent)
        {
            if (current.Stylesheet != null)
                return current.Stylesheet;
        }

        return null;
    }
}
