// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    public async Task<int?> FindRoundId(int serverId, int number)
    {
        await using var db = await GetDb();

        return await db.DbContext.Round
            .Where(round => round.ServerId == serverId && round.Number == number)
            .Select(round => (int?)round.Id)
            .SingleOrDefaultAsync();
    }
}
