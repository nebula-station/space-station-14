// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/dfbda555db9acabf3827cf3b0785c6deaa5c1a8b/Content.Server/Connection/ConnectionManager.ArcaneQueue.cs

using System.Threading.Tasks;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Shared.Network;

namespace Content.Server.Connection;

public sealed partial class ConnectionManager
{
    public async Task<bool> HasPrivilegedJoin(NetUserId user)
    {
        if (HasTemporaryBypass(user))
            return true;

        _ticker ??= _entityManager.SystemOrNull<ServerGameTicker>();
        if (_ticker != null && _ticker.PlayerGameStatuses.TryGetValue(user, out var status)
            && status == PlayerGameStatus.JoinedGame)
            return true;

        if (!_cfg.GetCVar(CCVars.AdminBypassMaxPlayers))
            return false;

        var admin = await _db.GetAdminDataForAsync(user);
        return admin != null && _cfg.GetCVar(CCVars.AdminBypassMaxPlayers);
    }
}
