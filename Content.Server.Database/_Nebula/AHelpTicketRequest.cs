// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

[Table("ahelp_ticket_request")]
[Index(nameof(RequestedAt))]
public sealed class AHelpTicketRequest
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [ForeignKey(nameof(Round))] public int? RoundId { get; set; }
    public Round? Round { get; set; }

    [ForeignKey(nameof(Server))] public int ServerId { get; set; }
    public Server Server { get; set; } = default!;

    public Guid TicketOwnerUserId { get; set; }

    public Guid AdminUserId { get; set; }

    public AHelpTicketRequestKind Kind { get; set; }

    public DateTime RequestedAt { get; set; }
}

public enum AHelpTicketRequestKind : short
{
    Close = 1,

    Briefing = 2,
}
