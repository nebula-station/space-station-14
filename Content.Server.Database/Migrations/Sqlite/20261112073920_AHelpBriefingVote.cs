using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AHelpBriefingVote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ahelp_briefing_vote",
                columns: table => new
                {
                    summary_id = table.Column<long>(type: "INTEGER", nullable: false),
                    admin_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    vote = table.Column<short>(type: "INTEGER", nullable: false),
                    voted_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ahelp_briefing_vote", x => new { x.summary_id, x.admin_user_id });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ahelp_briefing_vote");
        }
    }
}
