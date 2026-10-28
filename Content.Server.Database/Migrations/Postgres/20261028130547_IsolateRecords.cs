using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class IsolateRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "server_id",
                table: "ban",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "server_id",
                table: "admin_watchlists",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "server_id",
                table: "admin_notes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "server_id",
                table: "admin_messages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ban_server_id",
                table: "ban",
                column: "server_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_watchlists_server_id",
                table: "admin_watchlists",
                column: "server_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_notes_server_id",
                table: "admin_notes",
                column: "server_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_messages_server_id",
                table: "admin_messages",
                column: "server_id");

            migrationBuilder.AddForeignKey(
                name: "FK_admin_messages_server_server_id",
                table: "admin_messages",
                column: "server_id",
                principalTable: "server",
                principalColumn: "server_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_admin_notes_server_server_id",
                table: "admin_notes",
                column: "server_id",
                principalTable: "server",
                principalColumn: "server_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_admin_watchlists_server_server_id",
                table: "admin_watchlists",
                column: "server_id",
                principalTable: "server",
                principalColumn: "server_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ban_server_server_id",
                table: "ban",
                column: "server_id",
                principalTable: "server",
                principalColumn: "server_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_admin_messages_server_server_id",
                table: "admin_messages");

            migrationBuilder.DropForeignKey(
                name: "FK_admin_notes_server_server_id",
                table: "admin_notes");

            migrationBuilder.DropForeignKey(
                name: "FK_admin_watchlists_server_server_id",
                table: "admin_watchlists");

            migrationBuilder.DropForeignKey(
                name: "FK_ban_server_server_id",
                table: "ban");

            migrationBuilder.DropIndex(
                name: "IX_ban_server_id",
                table: "ban");

            migrationBuilder.DropIndex(
                name: "IX_admin_watchlists_server_id",
                table: "admin_watchlists");

            migrationBuilder.DropIndex(
                name: "IX_admin_notes_server_id",
                table: "admin_notes");

            migrationBuilder.DropIndex(
                name: "IX_admin_messages_server_id",
                table: "admin_messages");

            migrationBuilder.DropColumn(
                name: "server_id",
                table: "ban");

            migrationBuilder.DropColumn(
                name: "server_id",
                table: "admin_watchlists");

            migrationBuilder.DropColumn(
                name: "server_id",
                table: "admin_notes");

            migrationBuilder.DropColumn(
                name: "server_id",
                table: "admin_messages");
        }
    }
}
