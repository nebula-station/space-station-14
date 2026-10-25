// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/dead-space-server/EridaGoob/blob/312c4de3ed33142c2a488771f4d1f58bbc499974/Content.Server/_Orion/ServerProtection/ServerProtectionPunishmentSystem.cs

using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Connection;
using Content.Server.GhostKick;
using Content.Shared.Database;
using Robust.Shared.Player;

namespace Content.Server._Orion.ServerProtection;

/// <summary>
/// Punishments applied automatically by the server protection systems.
/// </summary>
public sealed partial class ServerProtectionPunishmentSystem : EntitySystem
{
    [Dependency] private IBanManager _banManager = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private GhostKickManager _ghostKick = default!;
    [Dependency] private AdminSystem _admin = default!;

    private ISawmill _log = default!;

    public override void Initialize()
    {
        base.Initialize();
        _log = Logger.GetSawmill("serverprotection.actions");
    }

    /// <summary>
    /// Bans the player by user id, address and hardware id.
    /// </summary>
    /// <param name="minutes">Ban duration, 0 or less for a permanent ban.</param>
    public void ApplyBan(ICommonSession player, string reason, int minutes = 0)
    {
        var banInfo = new CreateServerBanInfo(reason);
        banInfo.AddUser(player.UserId, player.Name);
        banInfo.AddAddress(player.Channel.RemoteEndPoint.Address);
        banInfo.AddHWId(player.Channel.UserData.GetModernHwid());
        banInfo.WithSeverity(NoteSeverity.High);
        if (minutes > 0)
            banInfo.WithMinutes(minutes);

        _banManager.CreateServerBan(banInfo);
        _log.Info($"{player.Name} был забанен: {reason}");
    }

    /// <summary>
    /// Kicks the player.
    /// </summary>
    public void KickPlayer(ICommonSession player, string reason)
    {
        _ghostKick.DoDisconnect(player.Channel, reason);
        _log.Info($"{player.Name} был кикнут: {reason}");
    }

    /// <summary>
    /// Deletes all messages sent by the player.
    /// </summary>
    public void DeleteMessages(ICommonSession player)
    {
        _chat.DeleteMessagesBy(player.UserId);
    }

    /// <summary>
    /// Erases the player's character the same way as the admin erase verb.
    /// </summary>
    public void EraseCharacter(ICommonSession player)
    {
        _admin.Erase(player.UserId);
    }

    /// <summary>
    /// Sends an alert to all online admins.
    /// </summary>
    public void SendAdminAlert(string message)
    {
        _chat.SendAdminAlert(message);
    }
}
