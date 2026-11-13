using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AHelpAdminAssist : Migration
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

            migrationBuilder.CreateTable(
                name: "ahelp_message",
                columns: table => new
                {
                    ahelp_message_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    round_id = table.Column<int>(type: "INTEGER", nullable: true),
                    server_id = table.Column<int>(type: "INTEGER", nullable: false),
                    ticket_owner_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    sender_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    sender_name = table.Column<string>(type: "TEXT", nullable: false),
                    sender_is_admin = table.Column<bool>(type: "INTEGER", nullable: false),
                    admin_only = table.Column<bool>(type: "INTEGER", nullable: false),
                    from_discord = table.Column<bool>(type: "INTEGER", nullable: false),
                    text = table.Column<string>(type: "TEXT", nullable: false),
                    sent_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ahelp_message", x => x.ahelp_message_id);
                    table.ForeignKey(
                        name: "FK_ahelp_message_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id");
                    table.ForeignKey(
                        name: "FK_ahelp_message_server_server_id",
                        column: x => x.server_id,
                        principalTable: "server",
                        principalColumn: "server_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ahelp_ticket_request",
                columns: table => new
                {
                    ahelp_ticket_request_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    round_id = table.Column<int>(type: "INTEGER", nullable: true),
                    server_id = table.Column<int>(type: "INTEGER", nullable: false),
                    ticket_owner_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    admin_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    kind = table.Column<short>(type: "INTEGER", nullable: false),
                    requested_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ahelp_ticket_request", x => x.ahelp_ticket_request_id);
                    table.ForeignKey(
                        name: "FK_ahelp_ticket_request_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id");
                    table.ForeignKey(
                        name: "FK_ahelp_ticket_request_server_server_id",
                        column: x => x.server_id,
                        principalTable: "server",
                        principalColumn: "server_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ahelp_message_round_id",
                table: "ahelp_message",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "IX_ahelp_message_server_id",
                table: "ahelp_message",
                column: "server_id");

            migrationBuilder.CreateIndex(
                name: "IX_ahelp_message_ticket_owner_user_id_ahelp_message_id",
                table: "ahelp_message",
                columns: new[] { "ticket_owner_user_id", "ahelp_message_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ahelp_ticket_request_requested_at",
                table: "ahelp_ticket_request",
                column: "requested_at");

            migrationBuilder.CreateIndex(
                name: "IX_ahelp_ticket_request_round_id",
                table: "ahelp_ticket_request",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "IX_ahelp_ticket_request_server_id",
                table: "ahelp_ticket_request",
                column: "server_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ahelp_briefing_vote");

            migrationBuilder.DropTable(
                name: "ahelp_message");

            migrationBuilder.DropTable(
                name: "ahelp_ticket_request");
        }
    }
}
