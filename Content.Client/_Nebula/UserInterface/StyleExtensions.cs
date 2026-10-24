// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.UserInterface;

namespace Content.Client._Nebula.UserInterface;

public static class StyleExtensions
{
    /// <summary>
    /// Reads a style property, falling back when it is missing or has the wrong type.
    /// </summary>
    public static T GetStyle<T>(this Control control, string property, T fallback)
    {
        return control.TryGetStyleProperty<T>(property, out var value) && value is not null
            ? value
            : fallback;
    }
}
