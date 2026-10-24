// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Player;

namespace Content.Shared._Nebula.ERP;

/// <summary>
/// Raised on an entity when its arousal hits the peak.
/// </summary>
/// <param name="PrivateFilter">Recipients of the effects when the interaction was private, null otherwise.</param>
[ByRefEvent]
public record struct ArousalOrgasmEvent(Filter? PrivateFilter);
