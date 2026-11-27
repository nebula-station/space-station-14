// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/b52ef462f193de8563488db184be5393b9881342/Content.Arcane.Server/ServerProtection/ServerProtectionPunishmentSystem.cs

using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Orion.ServerProtection;

/// <summary>
///     Punishments used by the emote and admin action protection (Orion #175, #318).
/// </summary>
public sealed partial class ServerProtectionPunishmentSystem
{
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private IPlayerLocator _locator = default!;
    [Dependency] private IPlayerManager _player = default!;

    /// <summary>
    ///     Bans a player by user id, with their last known address and HWID. 0 minutes is permanent.
    /// </summary>
    public async void ApplyBan(NetUserId userId, string username, string reason, int minutes = 0)
    {
        var banInfo = new CreateServerBanInfo(reason);
        banInfo.AddUser(userId, username);

        var located = await _locator.LookupIdAsync(userId);
        if (located != null)
        {
            banInfo.AddAddress(located.LastAddress);
            banInfo.AddHWId(located.LastHWId);
        }

        if (minutes > 0)
            banInfo.WithMinutes(minutes);
        banInfo.WithSeverity(NoteSeverity.High);

        _banManager.CreateServerBan(banInfo);
        Log.Info($"{username} ({userId}) was banned: {reason}");
    }

    public void DeAdmin(ICommonSession player, string reason)
    {
        _adminManager.DeAdmin(player);
        Log.Warning($"{player.Name} was de-admined: {reason}");
    }

    /// <summary>
    ///     Suspends the admin record, so neither readmin nor reconnecting gives the permissions back until an
    ///     authorized admin lifts the suspension in the permissions panel. Works for admins that already left,
    ///     their bans can finish after they disconnect. Admins without a database record (local or transient)
    ///     are de-admined instead.
    /// </summary>
    public async void Suspend(NetUserId userId, string name, string reason)
    {
        var admin = await _db.GetAdminDataForAsync(userId);
        if (admin != null && !admin.Suspended)
        {
            admin.Suspended = true;
            await _db.UpdateAdminAsync(admin);
        }

        if (admin != null)
            Log.Warning($"{name} ({userId}) was suspended as admin: {reason}");

        if (!_player.TryGetSessionById(userId, out var session))
            return;

        if (admin != null)
            _adminManager.ReloadAdmin(session);
        else if (_adminManager.IsAdmin(session, includeDeAdmin: true))
            DeAdmin(session, reason);
    }
}
