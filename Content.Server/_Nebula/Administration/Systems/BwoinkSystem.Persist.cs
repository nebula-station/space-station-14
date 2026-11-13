// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Database;
using Content.Shared._Nebula.CCVar;
using Content.Shared.Administration;

namespace Content.Server.Administration.Systems;

public sealed partial class BwoinkSystem
{
    [Dependency] private ServerDbEntryManager _serverDbEntry = default!;

    private async void PersistAHelpMessage(BwoinkParams bwoinkParams)
    {
        if (!_config.GetCVar(NCCVars.AHelpPersist))
            return;

        var roundId = _gameTicker.RoundDatabaseId;
        var message = new AHelpMessage
        {
            RoundId = roundId == 0 ? null : roundId,
            TicketOwnerUserId = bwoinkParams.Message.UserId,
            SenderUserId = bwoinkParams.SenderId,
            SenderName = bwoinkParams.SenderName,
            SenderIsAdmin = bwoinkParams.FromWebhook ||
                            (bwoinkParams.SenderAdmin?.HasFlag(AdminFlags.Adminhelp) ?? false),
            AdminOnly = bwoinkParams.Message.AdminOnly,
            FromDiscord = bwoinkParams.FromWebhook,
            Text = bwoinkParams.Message.Text,
            SentAt = DateTime.UtcNow,
        };

        try
        {
            message.ServerId = (await _serverDbEntry.ServerEntity).Id;
            await _dbManager.AddAHelpMessage(message);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to store A-Help message: {e}");
        }
    }
}
