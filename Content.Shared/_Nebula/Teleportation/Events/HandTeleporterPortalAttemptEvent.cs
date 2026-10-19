// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Shared._Nebula.Teleportation.Events;

[ByRefEvent]
public record struct HandTeleporterPortalAttemptEvent(EntityUid User)
{
    public bool Cancelled;

    public string? Reason;

    public void Cancel(string? reason = null)
    {
        Cancelled = true;
        Reason ??= reason;
    }
}
