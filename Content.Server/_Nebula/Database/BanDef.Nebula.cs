// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server.Database;

public sealed partial class BanDef
{
    // null = all servers
    public int? ServerId { get; set; }
}
