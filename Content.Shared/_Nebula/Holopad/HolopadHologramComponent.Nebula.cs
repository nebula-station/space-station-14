// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Holopad;

public sealed partial class HolopadHologramComponent
{
    /// <summary>
    /// Entity the client's sprite was last copied from.
    /// </summary>
    [ViewVariables]
    public EntityUid? SpriteSource;

    /// <summary>
    /// Client's real time of the next sprite copy.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextSpriteRefresh;
}
