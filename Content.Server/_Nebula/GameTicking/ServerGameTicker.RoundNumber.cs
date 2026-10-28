// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;

namespace Content.Server.GameTicking;

public sealed partial class ServerGameTicker
{
    public int RoundDatabaseId { get; private set; }

    public static int GetRoundDatabaseId(IEntitySystemManager esm)
    {
        return esm.GetEntitySystemOrNull<ServerGameTicker>()?.RoundDatabaseId ?? 0;
    }
}
