// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/dfbda555db9acabf3827cf3b0785c6deaa5c1a8b/Content.Server/GameTicking/Events/GetAdmittedPlayerCountEvent.Arcane.cs

namespace Content.Server.GameTicking.Events;

/// <summary>Allows a content queue to exclude waiting connections from population-dependent checks.</summary>
[ByRefEvent]
public struct GetAdmittedPlayerCountEvent(int count)
{
    public int Count = count;
}
