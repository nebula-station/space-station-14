// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/dfbda555db9acabf3827cf3b0785c6deaa5c1a8b/Content.Server/GameTicking/ServerGameTicker.ArcaneQueue.cs

using System.Linq;
using System.Threading;
using Content.Server.GameTicking.Events;
using Robust.Shared.Enums;

namespace Content.Server.GameTicking;

public sealed partial class ServerGameTicker
{
    private int _admittedPlayerCount;
    private int _admittedNonAdminPlayerCount;
    public int AdmittedPlayerCount => Volatile.Read(ref _admittedPlayerCount);
    public int AdmittedNonAdminPlayerCount => Volatile.Read(ref _admittedNonAdminPlayerCount);

    public int GetAdmittedPlayerCount()
    {
        var count = new GetAdmittedPlayerCountEvent(_playerManager.PlayerCount);
        RaiseLocalEvent(ref count);
        var admins = _adminManager.ActiveAdmins.Count(s => s.Status == SessionStatus.InGame);
        Volatile.Write(ref _admittedPlayerCount, count.Count);
        Volatile.Write(ref _admittedNonAdminPlayerCount, Math.Max(0, count.Count - admins));
        return count.Count;
    }
}
