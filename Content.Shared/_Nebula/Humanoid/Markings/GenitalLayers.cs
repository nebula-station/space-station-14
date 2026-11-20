// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid;

namespace Content.Shared._Nebula.Humanoid.Markings;

/// <summary>
/// Nebula - WolfGate. Marking layers that draw genitals; they are hidden with the nudity censor.
/// </summary>
public static class GenitalLayers
{
    public static bool IsGenital(HumanoidVisualLayers layer)
    {
        return layer is HumanoidVisualLayers.Breasts
            or HumanoidVisualLayers.BreastsBehind
            or HumanoidVisualLayers.Vagina
            or HumanoidVisualLayers.Penis;
    }
}
