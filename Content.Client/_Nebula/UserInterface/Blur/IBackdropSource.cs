// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Graphics;

namespace Content.Client._Nebula.UserInterface.Blur;

/// <summary>
/// Provides a blurred image of what is drawn behind translucent panels.
/// </summary>
public interface IBackdropSource
{
    /// <summary>
    /// Blurred image, or null while it is not ready.
    /// </summary>
    Texture? BackdropTexture { get; }

    /// <summary>
    /// Area of the screen in pixels that <see cref="BackdropTexture"/> covers.
    /// </summary>
    UIBox2 BackdropScreenRect { get; }
}
