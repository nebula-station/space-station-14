// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.Implants;

/// <summary>
/// Raised on a body before an autosurgeon installs cyberware of the given prototype into it.
/// </summary>
[ByRefEvent]
public record struct CyberwareInstallAttemptEvent(EntProtoId Cyberware, bool Cancelled = false, string? Reason = null);
