// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    public async Task AddAHelpMessage(AHelpMessage message)
    {
        await using var db = await GetDb();

        db.DbContext.AHelpMessage.Add(message);
        await db.DbContext.SaveChangesAsync();
    }

    public async Task<List<AHelpMessage>> GetAHelpMessages(Guid ticketOwner, int? roundId)
    {
        await using var db = await GetDb();

        return await db.DbContext.AHelpMessage
            .AsNoTracking()
            .Where(message => message.TicketOwnerUserId == ticketOwner && message.RoundId == roundId)
            .OrderBy(message => message.Id)
            .ToListAsync();
    }
}
