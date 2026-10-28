// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

// dont filter if null scope. otherwise keep null rows and this server
public static class RecordScope
{
    public static IQueryable<T> InScope<T>(this IQueryable<T> query, int? scopeServerId)
        where T : class
    {
        if (scopeServerId is not int id)
            return query;

        return query.Where(row =>
            EF.Property<int?>(row, nameof(IServerScopedRecord.ServerId)) == null ||
            EF.Property<int?>(row, nameof(IServerScopedRecord.ServerId)) == id);
    }

    public static bool Matches(int? recordServerId, int? scopeServerId)
    {
        return scopeServerId is not int id || recordServerId == null || recordServerId == id;
    }
}
