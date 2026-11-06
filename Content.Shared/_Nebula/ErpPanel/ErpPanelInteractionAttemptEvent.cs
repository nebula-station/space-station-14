// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Nebula.ErpPanel;

/// <summary>
/// Raised on the user before an ERP panel interaction runs, after the usual checks passed.
/// Cancelling it stops the interaction, whoever cancels shows the popup.
/// </summary>
[ByRefEvent]
public record struct ErpPanelInteractionAttemptEvent(EntityUid Target, bool Erotic, bool Cancelled = false);
