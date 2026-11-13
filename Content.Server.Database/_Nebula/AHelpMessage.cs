// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

[Table("ahelp_message")]
[Index(nameof(TicketOwnerUserId), nameof(Id))]
[Index(nameof(RoundId))]
public sealed class AHelpMessage
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [ForeignKey(nameof(Round))] public int? RoundId { get; set; }
    public Round? Round { get; set; }

    [ForeignKey(nameof(Server))] public int ServerId { get; set; }
    public Server Server { get; set; } = default!;

    public Guid TicketOwnerUserId { get; set; }

    public Guid SenderUserId { get; set; }

    public string SenderName { get; set; } = default!;

    public bool SenderIsAdmin { get; set; }

    public bool AdminOnly { get; set; }

    public bool FromDiscord { get; set; }

    public string Text { get; set; } = default!;

    public DateTime SentAt { get; set; }
}
