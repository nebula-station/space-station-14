// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

[Table("ahelp_briefing_vote")]
[PrimaryKey(nameof(SummaryId), nameof(AdminUserId))]
public sealed class AHelpBriefingVote
{
    public long SummaryId { get; set; }

    public Guid AdminUserId { get; set; }

    public short Vote { get; set; }

    public DateTime VotedAt { get; set; }
}
