// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Nebula.ErpPanel;

/// <summary>
/// Lets the entity open the interaction panel and be its target.
/// </summary>
[RegisterComponent]
public sealed partial class ErpPanelOwnerComponent : Component
{
    public static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Upper bound of the per-player arousal and moan multipliers, in percent.
    /// </summary>
    public const float MaxMultiplier = 200f;

    public EntityUid? Target;

    public TimeSpan LastInteraction;
}
