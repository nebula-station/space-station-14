// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Nebula.Chemistry;

[ByRefEvent]
public record struct HarvestSolutionAttemptEvent(EntityUid User, EntityUid Container, bool Popup)
{
    public bool Cancelled;
}
