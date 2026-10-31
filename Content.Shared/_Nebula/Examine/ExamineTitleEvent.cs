// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Nebula.Examine;

/// <summary>
/// Raised on the client on an examined entity before its tooltip is shown.
/// <see cref="Markup"/> is the title, already escaped, and is rendered in bold.
/// </summary>
[ByRefEvent]
public record struct ExamineTitleEvent(EntityUid Examiner, string Markup);
