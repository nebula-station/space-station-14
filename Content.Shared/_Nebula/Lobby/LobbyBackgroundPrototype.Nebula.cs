// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.GameTicking.Prototypes;

/// <summary>
/// Animated lobby backgrounds, ported from Monolith (LuaM).
/// An animated background is a folder of frames named 000, 001, 002 and so on.
/// <see cref="Background"/> points at the first frame, so it still works as a static image.
/// </summary>
public sealed partial class LobbyBackgroundPrototype
{
    /// <summary>
    /// Number of frames. 0 or 1 means a static background.
    /// </summary>
    [DataField]
    public int Frames;

    /// <summary>
    /// Seconds each frame is shown, unless <see cref="Delays"/> says otherwise.
    /// </summary>
    [DataField]
    public float FrameTime = 0.1f;

    /// <summary>
    /// Optional per frame delays in seconds.
    /// </summary>
    [DataField]
    public List<float> Delays = new();

    public bool Animated => Frames > 1;
}
