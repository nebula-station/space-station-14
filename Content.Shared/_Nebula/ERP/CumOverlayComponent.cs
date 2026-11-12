// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared._Nebula.ERP;

/// <summary>
/// Fluid stains drawn over a mob after someone finished on it.
/// Washed off by water or space cleaner, removed by rejuvenate and dries out on its own.
/// Ported from Arcane and reworked.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(CumOverlaySystem))]
public sealed partial class CumOverlayComponent : Component
{
    public static readonly ResPath OverlayRsi = new("/Textures/_Arcane/Effects/cumoverlay.rsi");

    public const string NormalState = "cum_normal";
    public const string LargeState = "cum_large";

    /// <summary>
    /// How many times the mob was hit. 1 draws the normal state, 2 and more draw the large one.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Count;

    /// <summary>
    /// When the stain dries out and disappears by itself.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan DriesAt;

    /// <summary>
    /// How long a fresh stain stays before drying out. Every new hit refreshes the timer.
    /// </summary>
    [DataField]
    public TimeSpan DryTime = TimeSpan.FromMinutes(20);
}

public enum CumOverlayLayer : byte
{
    Base,
}
