// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public interface IServerScopedRecord
{
    int? ServerId { get; set; }
}

public sealed partial class Ban : IServerScopedRecord
{
    public int? ServerId { get; set; }

    public Server? Server { get; set; }
}

public partial class AdminNote : IServerScopedRecord
{
    public int? ServerId { get; set; }

    public Server? Server { get; set; }
}

public partial class AdminWatchlist : IServerScopedRecord
{
    public int? ServerId { get; set; }

    public Server? Server { get; set; }
}

public partial class AdminMessage : IServerScopedRecord
{
    public int? ServerId { get; set; }

    public Server? Server { get; set; }
}

internal static class RecordScopeModel
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ban>()
            .HasOne(b => b.Server)
            .WithMany()
            .HasForeignKey(b => b.ServerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AdminNote>()
            .HasOne(n => n.Server)
            .WithMany()
            .HasForeignKey(n => n.ServerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AdminWatchlist>()
            .HasOne(w => w.Server)
            .WithMany()
            .HasForeignKey(w => w.ServerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AdminMessage>()
            .HasOne(m => m.Server)
            .WithMany()
            .HasForeignKey(m => m.ServerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
