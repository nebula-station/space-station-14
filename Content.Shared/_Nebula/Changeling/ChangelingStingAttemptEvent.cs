// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Nebula.Changeling;

/// <summary>
/// Raised on a changeling before any of its stings hits a target.
/// Cancelling it stops the sting, whoever cancels shows the popup.
/// </summary>
[ByRefEvent]
public record struct ChangelingStingAttemptEvent(EntityUid Target, EntityUid Action, bool Cancelled = false);
